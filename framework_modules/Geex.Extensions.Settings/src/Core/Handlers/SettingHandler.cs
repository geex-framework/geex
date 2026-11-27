using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using Geex.Extensions.Authentication;
using Geex.Extensions.Settings.Core;
using Geex.Extensions.Settings.Requests;
using Geex.MultiTenant;

using MediatX;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using MongoDB.Entities;

using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.Settings.Core.Handlers
{
    public class SettingHandler : ISettingService
    {
        private static IReadOnlyList<SettingDefinition> _settingDefinitions;
        private readonly LazyService<ICurrentTenant> _currentTenant;
        private readonly ICurrentUser _currentUser;
        private readonly DbContext _dbContext;
        private readonly IRedisDatabase _redisClient;
        private readonly Dictionary<string, Setting?> _pendingRedisSync = new();

        public SettingHandler(IRedisDatabase redisClient, DbContext dbContext, ICurrentUser currentUser, ILogger<SettingHandler> logger, LazyService<ICurrentTenant> currentTenant)
        {
            Logger = logger;
            _redisClient = redisClient;
            _dbContext = dbContext;
            _currentUser = currentUser;
            _currentTenant = currentTenant;
            _dbContext.PersistingTransaction += PersistPendingChanges;
            _dbContext.PostSaveChanges += OnSaved;
            _dbContext.TransactionAborted += ClearPending;
            if (_settingDefinitions == default)
            {
                _settingDefinitions = new ReadOnlyCollection<SettingDefinition>(SettingDefinition.List.ToArray());
            }
        }

        public ILogger<SettingHandler> Logger { get; }

        public async Task<List<Setting>> GetActiveSettings()
        {
            var merged = new Dictionary<SettingDefinition, Setting>();
            foreach (var (scope, key) in EffectiveScopes(_currentUser.UserId, _currentTenant.Value?.Code))
            {
                foreach (var item in await FetchScopeSettings(scope, key))
                    if (item.Name.ValidScopes.Contains(scope)) merged.TryAdd(item.Name, item);
            }
            return ApplySharedDefaults(merged.Values, SettingScopeEnumeration.Global, null);
        }

        public async Task<List<Setting>> GetGlobalSettings()
        {
            var result = await FetchScopeSettings(SettingScopeEnumeration.Global, null);
            return ApplySharedDefaults(result, SettingScopeEnumeration.Global, null);
        }

        public async Task<Setting?> GetSetting(SettingDefinition settingDefinition, SettingScopeEnumeration settingScope = default,
            string? scopedKey = default)
        {
            return await ResolveSettingAsync(settingDefinition, settingScope, scopedKey);
        }

        public async Task<List<Setting>> GetTenantSettings()
        {
            var tenantCode = _currentTenant?.Value?.Code;
            if (tenantCode.IsNullOrEmpty())
            {
                return new List<Setting>();
            }

            var result = await FetchScopeSettings(SettingScopeEnumeration.Tenant, tenantCode);
            return ApplySharedDefaults(result, SettingScopeEnumeration.Tenant, tenantCode);
        }

        public async Task<List<Setting>> GetUserSettings()
        {
            var userId = _currentUser.UserId;
            if (userId == null || userId.IsNullOrEmpty())
            {
                return new List<Setting>();
            }

            var result = await FetchScopeSettings(SettingScopeEnumeration.User, userId);
            return ApplySharedDefaults(result, SettingScopeEnumeration.User, userId);
        }

        public async Task<Setting> SetAsync(SettingDefinition settingDefinition, SettingScopeEnumeration scope, string? scopedKey, JsonNode? value, CancellationToken cancellationToken = default)
        {
            var definition = SettingDefinitions.FirstOrDefault(x => x.Name == settingDefinition);
            if (definition == default)
            {
                throw new BusinessException(GeexExceptionType.NotFound, message: "setting name not exists.");
            }

            if (!definition.ValidScopes.Contains(scope))
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: $"scope [{scope}] is not valid for setting [{settingDefinition}].");
            }

            scopedKey = await WriteScopeKey(scope, scopedKey, cancellationToken);
            await definition.ValidateAsync(value, _dbContext.ServiceProvider, cancellationToken);
            var key = new Setting(definition, null, scope, scopedKey).GetRedisKey();
            if (!_pendingRedisSync.TryGetValue(key, out var setting))
                setting = _dbContext.Query<Setting>().SingleOrDefault(x => x.Name == definition && x.Scope == scope && x.ScopedKey == scopedKey);
            if (setting == default)
            {
                setting = _dbContext.Attach(new Setting(settingDefinition, value, scope, scopedKey));
            }
            else
            {
                setting.SetValue(value);
            }

            _pendingRedisSync[key] = setting;
            return setting;
        }

        private async Task<string?> WriteScopeKey(SettingScopeEnumeration scope, string? key, CancellationToken token)
        {
            var currentTenant = _currentTenant.Value?.Code;
            if (scope == SettingScopeEnumeration.Global)
            {
                if (!string.IsNullOrEmpty(currentTenant))
                    throw new BusinessException(GeexExceptionType.ValidationFailed, message: "cannot update global setting in tenant.");
                return null;
            }
            if (scope != SettingScopeEnumeration.Tenant) return key;
            if (!string.IsNullOrEmpty(currentTenant))
            {
                if (!string.IsNullOrEmpty(key) && key != currentTenant)
                    throw new BusinessException(GeexExceptionType.ValidationFailed, message: "只能修改当前租户的设置.");
                return currentTenant;
            }
            if (string.IsNullOrWhiteSpace(key))
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "宿主代管租户设置时必须指定目标租户.");
            if (!_currentUser.IsSuperAdmin && (string.IsNullOrEmpty(_currentUser.UserId) ||
                !await _dbContext.ServiceProvider.GetRequiredService<IUnitOfWork>()
                    .Request(new CheckSubjectPermissionRequest(_currentUser.UserId, SettingsPermission.Edit), token)))
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "需要设置编辑权限才能代管租户.");
            return key;
        }

        public async Task<bool> RestoreTenantInheritanceAsync(SettingDefinition definition, string? tenantCode, CancellationToken cancellationToken = default)
        {
            if (!SupportsTenantInheritance(definition))
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "该设置不支持继承全局值.");
            var scope = SettingScopeEnumeration.Tenant;
            var key = await WriteScopeKey(scope, tenantCode, cancellationToken);
            var cacheKey = new Setting(definition, null, scope, key).GetRedisKey();
            if (!_pendingRedisSync.TryGetValue(cacheKey, out var setting))
                setting = _dbContext.Query<Setting>().SingleOrDefault(item => item.Name == definition && item.Scope == scope && item.ScopedKey == key);
            if (setting != null)
                await _dbContext.ServiceProvider.GetRequiredService<IUnitOfWork>().DbContext.RemoveOnSave(setting, cancellationToken);
            _pendingRedisSync[cacheKey] = null;
            return true;
        }

        public async Task<bool> CanRestoreTenantInheritance(SettingDefinition definition)
        {
            var key = _currentTenant.Value?.Code;
            return !string.IsNullOrEmpty(key) && SupportsTenantInheritance(definition) &&
                   await TryGetStoredSettingAsync(definition, SettingScopeEnumeration.Tenant, key) != null;
        }

        private static bool SupportsTenantInheritance(SettingDefinition definition) =>
            definition.ValidScopes.Contains(SettingScopeEnumeration.Global) && definition.ValidScopes.Contains(SettingScopeEnumeration.Tenant);

        private Task PersistPendingChanges(CancellationToken token)
        {
            if (_pendingRedisSync.Count > 0 && _dbContext.Session.IsInTransaction)
                _dbContext.AfterCommit(this, FlushPendingRedisSyncAsync);
            return Task.CompletedTask;
        }

        private Task OnSaved() => _dbContext.SupportTransaction ? Task.CompletedTask : FlushPendingRedisSyncAsync();

        private void ClearPending()
        {
            _pendingRedisSync.Clear();
        }

        private async Task FlushPendingRedisSyncAsync()
        {
            var pending = _pendingRedisSync.ToArray();
            ClearPending();
            foreach (var (key, _) in pending) await InvalidateCache(key);
        }

        private async Task InvalidateCache(string key)
        {
            try
            {
                await _redisClient.RemoveAsync(key);
            }
            catch (Exception error) { Logger.LogError(error, "Failed to synchronize setting cache {Key}.", key); }
        }

        private async Task<List<Setting>> FetchScopeSettings(SettingScopeEnumeration scope, string? scopedKey)
        {
            if (scope == SettingScopeEnumeration.Tenant && scopedKey.IsNullOrEmpty())
            {
                return new List<Setting>();
            }

            if (scope == SettingScopeEnumeration.User && scopedKey.IsNullOrEmpty())
            {
                return new List<Setting>();
            }

            if (_pendingRedisSync.Count > 0 || _dbContext.Session.IsInTransaction)
            {
                var result = _dbContext.Query<Setting>().Where(x => x.Scope == scope && x.ScopedKey == scopedKey)
                    .ToList().ToDictionary(item => item.GetRedisKey());
                foreach (var (key, value) in _pendingRedisSync)
                {
                    if (value == null) result.Remove(key);
                    else if (value.Scope == scope && value.ScopedKey == scopedKey) result[key] = value;
                }
                return result.Values.ToList();
            }

            var searchPattern = scopedKey.IsNullOrEmpty()
                ? $"Setting:{scope}:*"
                : $"Setting:{scope}:{scopedKey}:*";
            var cachedSettings = await _redisClient.GetAllFromRedisByPatternAsync(searchPattern, Logger);
            if (SettingDefinitions.Except(cachedSettings.Select(x => x.Value.Name)).Any())
            {
                var dbSettings = _dbContext.Query<Setting>()
                    .Where(x => x.Scope == scope && x.ScopedKey == scopedKey)
                    .ToList();
                await TrySyncSettings(cachedSettings, dbSettings);
                return dbSettings;
            }

            return cachedSettings.Values.ToList();
        }

        private async Task TrySyncSettings(IDictionary<string, Setting> cachedSettings, List<Setting> dbSettings)
        {
            var cachedByName = cachedSettings.Values.ToDictionary(x => x.Name, x => x);
            var needsSync = dbSettings.Any(db => !cachedByName.TryGetValue(db.Name, out var cached) || !SettingValuesEqual(cached, db))
                || cachedSettings.Values.Any(cached => dbSettings.All(db => db.Name != cached.Name));

            if (!needsSync)
            {
                return;
            }

            if (cachedSettings.Keys.Count > 0)
            {
                await _redisClient.RemoveAllAsync(cachedSettings.Keys.ToArray());
            }

            foreach (var setting in dbSettings)
            {
                await _redisClient.SetToRedisAsync(setting);
            }
        }

        private async Task<Setting?> ResolveSettingAsync(SettingDefinition settingDefinition, SettingScopeEnumeration settingScope, string? scopedKey)
        {
            var userId = _currentUser.UserId;
            var tenantCode = scopedKey ?? _currentTenant?.Value?.Code;

            foreach (var (scope, key) in EffectiveScopes(userId, tenantCode))
            {
                if (!settingDefinition.ValidScopes.Contains(scope)) continue;
                var stored = await TryGetStoredSettingAsync(settingDefinition, scope, key);
                if (stored != null) return stored;
            }
            return CreateDefaultSetting(settingDefinition, settingScope, scopedKey, userId, tenantCode);
        }

        private static IEnumerable<(SettingScopeEnumeration Scope, string? Key)> EffectiveScopes(string? userId, string? tenantCode)
        {
            if (!string.IsNullOrEmpty(userId)) yield return (SettingScopeEnumeration.User, userId);
            if (!string.IsNullOrEmpty(tenantCode)) yield return (SettingScopeEnumeration.Tenant, tenantCode);
            yield return (SettingScopeEnumeration.Global, null);
        }

        private async Task<Setting?> TryGetStoredSettingAsync(SettingDefinition settingDefinition, SettingScopeEnumeration scope, string? scopedKey)
        {
            var requested = new Setting(settingDefinition, null, scope, scopedKey);
            if (_pendingRedisSync.TryGetValue(requested.GetRedisKey(), out var pending)) return pending;
            if (_dbContext.Session.IsInTransaction)
                return _dbContext.Query<Setting>().SingleOrDefault(item => item.Name == settingDefinition && item.Scope == scope && item.ScopedKey == scopedKey);
            var redisSetting = await _redisClient.GetFromRedisAsync(requested);
            if (redisSetting != null)
            {
                return redisSetting;
            }

            var dbSetting = _dbContext.Query<Setting>().SingleOrDefault(x => x.Name == settingDefinition && x.Scope == scope && x.ScopedKey == scopedKey);
            if (dbSetting == null)
            {
                return null;
            }

            try
            {
                await _redisClient.SetToRedisAsync(dbSetting);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to warm redis cache for setting. {settings}", new { dbSetting.Scope, dbSetting.Name, dbSetting.ScopedKey, dbSetting.Value }.ToJsonSafe());
            }

            return dbSetting;
        }

        private static Setting CreateDefaultSetting(
            SettingDefinition settingDefinition,
            SettingScopeEnumeration settingScope,
            string? scopedKey,
            string? userId,
            string? tenantCode)
        {
            var fallbackScope = settingScope ?? SettingScopeEnumeration.Global;
            var fallbackScopedKey = fallbackScope.Switch()
                .Case(SettingScopeEnumeration.User, () => scopedKey ?? userId)
                .Case(SettingScopeEnumeration.Tenant, () => scopedKey ?? tenantCode)
                .Default(() => scopedKey);

            return new Setting(settingDefinition, settingDefinition.DefaultValue, fallbackScope, fallbackScopedKey);
        }

        private static List<Setting> ApplySharedDefaults(IEnumerable<Setting> settings, SettingScopeEnumeration scope, string? scopedKey)
        {
            var result = settings.ToList();
            var existingNames = result.Select(x => x.Name).ToHashSet();
            foreach (var definition in _settingDefinitions)
            {
                if (existingNames.Contains(definition))
                {
                    continue;
                }

                if (definition.DefaultValue == null)
                {
                    continue;
                }

                result.Add(new Setting(definition, definition.DefaultValue, scope, scopedKey));
            }

            return result;
        }

        private static bool SettingValuesEqual(Setting left, Setting right)
        {
            if (left.Value == null && right.Value == null)
            {
                return true;
            }

            if (left.Value == null || right.Value == null)
            {
                return false;
            }

            return left.Value.ToJsonString() == right.Value.ToJsonString();
        }

        async Task<List<ISetting>> ISettingService.GetActiveSettings() => (await GetActiveSettings()).Cast<ISetting>().ToList();

        async Task<List<ISetting>> ISettingService.GetGlobalSettings() => (await GetGlobalSettings()).Cast<ISetting>().ToList();

        async Task<ISetting> ISettingService.GetSetting(SettingDefinition settingDefinition,
            SettingScopeEnumeration settingScope, string? scopedKey)
        {
            var setting = await ResolveSettingAsync(settingDefinition, settingScope, scopedKey);
            return setting ?? CreateDefaultSetting(settingDefinition, settingScope, scopedKey, _currentUser.UserId, _currentTenant?.Value?.Code);
        }

        async Task<List<ISetting>> ISettingService.GetTenantSettings() => (await GetTenantSettings()).Cast<ISetting>().ToList();

        async Task<List<ISetting>> ISettingService.GetUserSettings() => (await GetUserSettings()).Cast<ISetting>().ToList();

        async Task<ISetting> ISettingService.SetAsync(SettingDefinition settingDefinition, SettingScopeEnumeration scope, string? scopedKey, JsonNode? value) => await SetAsync(settingDefinition, scope, scopedKey, value);

        public IReadOnlyList<SettingDefinition> SettingDefinitions => _settingDefinitions;
    }

    internal sealed class SettingRequestHandler(SettingHandler settings) :
        IRequestHandler<EditSettingRequest, ISetting>,
        IRequestHandler<GetSettingsRequest, IQueryable<ISetting>>,
        IRequestHandler<GetActiveSettingsRequest, List<ISetting>>
    {
        private static IEnumerable<Setting> ExcludeHiddenForClients(IEnumerable<Setting> settings)
        {
            return settings.Where(x => !x.Name.IsHiddenForClients);
        }

        public async Task<ISetting> Handle(EditSettingRequest request, CancellationToken cancellationToken)
        {
            return await settings.SetAsync(request.Name, request.Scope, request.ScopedKey, request.Value, cancellationToken);
        }

        public async Task<List<ISetting>> Handle(GetActiveSettingsRequest request, CancellationToken cancellationToken)
        {
            var settingValues = ExcludeHiddenForClients(await settings.GetActiveSettings());
            return settingValues.Cast<ISetting>().ToList();
        }

        public async Task<IQueryable<ISetting>> Handle(GetSettingsRequest request, CancellationToken cancellationToken)
        {
            var settingValues = Enumerable.Empty<Setting>();
            if (request.Scope != default)
            {
                settingValues = await request.Scope.Switch()
                    .Case(SettingScopeEnumeration.User, async () => await settings.GetUserSettings())
                    .Case(SettingScopeEnumeration.Global, async () => await settings.GetGlobalSettings())
                    .Case(SettingScopeEnumeration.Tenant, async () => await settings.GetTenantSettings())
                    .Default(async () => new List<Setting>());
            }
            else
            {
                settingValues = await settings.GetActiveSettings();
            }

            settingValues = settingValues.WhereIf(!request.SettingDefinitions.IsNullOrEmpty(), x => request.SettingDefinitions.Contains(x.Name));
            settingValues = settingValues.WhereIf(!request.FilterByName.IsNullOrEmpty(), x => x.Name.Name.Contains(request.FilterByName, StringComparison.InvariantCultureIgnoreCase));
            settingValues = ExcludeHiddenForClients(settingValues);
            return settingValues.AsQueryable();
        }
    }
}

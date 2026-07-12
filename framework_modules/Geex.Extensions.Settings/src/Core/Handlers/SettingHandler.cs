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

using MongoDB.Entities;

using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.Settings.Core.Handlers
{
    public class SettingHandler : ISettingService,
        IRequestHandler<EditSettingRequest, ISetting>,
        IRequestHandler<GetSettingsRequest, IQueryable<ISetting>>,
        IRequestHandler<GetActiveSettingsRequest, List<ISetting>>
    {
        private static IReadOnlyList<SettingDefinition> _settingDefinitions;
        private readonly LazyService<ICurrentTenant> _currentTenant;
        private readonly ICurrentUser _currentUser;
        private readonly DbContext _dbContext;
        private readonly IRedisDatabase _redisClient;
        private readonly Dictionary<string, Setting> _pendingRedisSync = new();

        public SettingHandler(IRedisDatabase redisClient, IEnumerable<GeexModule> modules, DbContext dbContext, ICurrentUser currentUser, ILogger<SettingHandler> logger, LazyService<ICurrentTenant> currentTenant)
        {
            Logger = logger;
            _redisClient = redisClient;
            _dbContext = dbContext;
            _currentUser = currentUser;
            _currentTenant = currentTenant;
            _dbContext.PostSaveChanges += FlushPendingRedisSyncAsync;
            if (_settingDefinitions == default)
            {
                _settingDefinitions = new ReadOnlyCollection<SettingDefinition>(SettingDefinition.List.ToArray());
            }
        }

        public ILogger<SettingHandler> Logger { get; }

        public async Task<List<Setting>> GetActiveSettings()
        {
            var globalSettings = await FetchScopeSettings(SettingScopeEnumeration.Global, null);
            var tenantSettings = await FetchScopeSettings(SettingScopeEnumeration.Tenant, _currentTenant?.Value?.Code);
            var userSettings = await FetchScopeSettings(SettingScopeEnumeration.User, _currentUser.UserId);

            var merged = userSettings
                .Union(tenantSettings, new GenericEqualityComparer<Setting>().With(x => x.Name))
                .Union(globalSettings, new GenericEqualityComparer<Setting>().With(x => x.Name));

            return ApplySharedDefaults(merged, SettingScopeEnumeration.Global, null);
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

        public async Task<Setting> SetAsync(SettingDefinition settingDefinition, SettingScopeEnumeration scope, string? scopedKey, JsonNode? value)
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

            var isEmptyTenant = string.IsNullOrEmpty(_currentTenant.Value?.Code);
            if (isEmptyTenant != true && scope == SettingScopeEnumeration.Global)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "cannot update global setting in tenant.");
            }

            var setting = _dbContext.Query<Setting>().SingleOrDefault(x => x.Name == settingDefinition && x.Scope == scope && x.ScopedKey == scopedKey);
            if (setting == default)
            {
                setting = _dbContext.Attach(new Setting(settingDefinition, value, scope, scopedKey));
            }
            else
            {
                setting.SetValue(value);
            }

            _pendingRedisSync[setting.GetRedisKey()] = setting;
            return setting;
        }

        private async Task FlushPendingRedisSyncAsync()
        {
            if (_pendingRedisSync.Count == 0)
            {
                return;
            }

            var pending = _pendingRedisSync.Values.ToList();
            _pendingRedisSync.Clear();

            foreach (var setting in pending)
            {
                try
                {
                    await _redisClient.SetToRedisAsync(setting);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failed to update redis cache for settings. {settings}", new { setting.Scope, setting.Name, setting.ScopedKey, setting.Value }.ToJsonSafe());
                }
            }
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

            var searchPattern = scopedKey.IsNullOrEmpty()
                ? $"Setting:{scope}:*"
                : $"Setting:{scope}:{scopedKey}:*";
            var cachedSettings = await _redisClient.GetAllFromRedisByPatternAsync(searchPattern);
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

            if (!userId.IsNullOrEmpty())
            {
                var userSetting = await TryGetStoredSettingAsync(settingDefinition, SettingScopeEnumeration.User, userId);
                if (userSetting != null)
                {
                    return userSetting;
                }
            }

            if (!tenantCode.IsNullOrEmpty())
            {
                var tenantSetting = await TryGetStoredSettingAsync(settingDefinition, SettingScopeEnumeration.Tenant, tenantCode);
                if (tenantSetting != null)
                {
                    return tenantSetting;
                }
            }

            var globalSetting = await TryGetStoredSettingAsync(settingDefinition, SettingScopeEnumeration.Global, null);
            if (globalSetting != null)
            {
                return globalSetting;
            }

            return CreateDefaultSetting(settingDefinition, settingScope, scopedKey, userId, tenantCode);
        }

        private async Task<Setting?> TryGetStoredSettingAsync(SettingDefinition settingDefinition, SettingScopeEnumeration scope, string? scopedKey)
        {
            var redisSetting = await _redisClient.GetFromRedisAsync(new Setting(settingDefinition, default, scope, scopedKey));
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

        private static IEnumerable<Setting> ExcludeHiddenForClients(IEnumerable<Setting> settings)
        {
            return settings.Where(x => !x.Name.IsHiddenForClients);
        }

        public virtual async Task<ISetting> Handle(EditSettingRequest request, CancellationToken cancellationToken)
        {
            return await SetAsync(request.Name, request.Scope, request.ScopedKey, request.Value);
        }

        public async Task<List<ISetting>> Handle(GetActiveSettingsRequest request, CancellationToken cancellationToken)
        {
            var settingValues = ExcludeHiddenForClients(await GetActiveSettings());
            return settingValues.Cast<ISetting>().ToList();
        }

        public virtual async Task<IQueryable<ISetting>> Handle(GetSettingsRequest request, CancellationToken cancellationToken)
        {
            var settingValues = Enumerable.Empty<Setting>();
            if (request.Scope != default)
            {
                settingValues = await request.Scope.Switch()
                    .Case(SettingScopeEnumeration.User, async () => await GetUserSettings())
                    .Case(SettingScopeEnumeration.Global, async () => await GetGlobalSettings())
                    .Case(SettingScopeEnumeration.Tenant, async () => await GetTenantSettings())
                    .Default(async () => new List<Setting>());
            }
            else
            {
                settingValues = await GetActiveSettings();
            }

            settingValues = settingValues.WhereIf(!request.SettingDefinitions.IsNullOrEmpty(), x => request.SettingDefinitions.Contains(x.Name));
            settingValues = settingValues.WhereIf(!request.FilterByName.IsNullOrEmpty(), x => x.Name.Name.Contains(request.FilterByName, StringComparison.InvariantCultureIgnoreCase));
            settingValues = ExcludeHiddenForClients(settingValues);
            return settingValues.AsQueryable();
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
}

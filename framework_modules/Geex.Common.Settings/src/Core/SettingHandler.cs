using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Abstractions;
using Geex.Common.Settings.Abstraction;
using Geex.Common.Settings.Api.Aggregates.Settings;
using Geex.Common.Requests.Settings;
using MediatR;
using Microsoft.Extensions.Logging;

using MongoDB.Entities;

using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Common.Settings.Core
{
    public class SettingHandler : IRequestHandler<EditSettingRequest, ISetting>,
        IRequestHandler<GetSettingsRequest, IQueryable<ISetting>>,
         IRequestHandler<GetInitSettingsRequest, List<ISetting>>
    {
        public ILogger<SettingHandler> Logger { get; }
        private IRedisDatabase _redisClient;
        private readonly DbContext _dbContext;
        private readonly LazyService<ClaimsPrincipal> _principal;
        private readonly LazyService<ICurrentTenant> _currentTenant;
        private static IReadOnlyList<SettingDefinition> _settingDefinitions;
        private static IReadOnlyList<Setting> _settingDefaults;

        private static IReadOnlyList<Setting> SettingDefaults => _settingDefaults ??= _settingDefinitions?.Select(x => x.DefaultInstance).ToList();


        public SettingHandler(IRedisDatabase redisClient, IEnumerable<GeexModule> modules, DbContext dbContext, LazyService<ClaimsPrincipal> principal, ILogger<SettingHandler> logger, LazyService<ICurrentTenant> currentTenant)
        {
            Logger = logger;
            _redisClient = redisClient;
            _dbContext = dbContext;
            _principal = principal;
            _currentTenant = currentTenant;
            if (_settingDefinitions == default)
            {
                var definitionTypes = modules
                   .Select(y => y.GetType().Assembly).Distinct()
                   .SelectMany(y => y.DefinedTypes
                   .Where(z => z.BaseType == typeof(SettingDefinition)));
                var settingDefinitions =
                    definitionTypes.SelectMany(x => x.GetProperties(BindingFlags.Static | BindingFlags.Public).Where(y => y.DeclaringType.IsAssignableTo(x))).Select(x => x.GetValue(null)).Cast<SettingDefinition>();
                _settingDefinitions = new ReadOnlyCollection<SettingDefinition>(settingDefinitions.ToArray());
            }

        }

        public IReadOnlyList<SettingDefinition> SettingDefinitions => _settingDefinitions;

        public async Task<Setting> SetAsync(SettingDefinition settingDefinition, SettingScopeEnumeration scope, string? scopedKey, JsonNode? value)
        {
            var definition = this.SettingDefinitions.FirstOrDefault(x => x.Name == settingDefinition);
            if (definition == default)
            {
                throw new BusinessException(GeexExceptionType.NotFound, message: "setting name not exists.");
            }

            if (_currentTenant.Value?.Code.IsNullOrEmpty() != true && scope == SettingScopeEnumeration.Global)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "cannot update global setting in tenant.");
            }

            if (_currentTenant.Value?.Code.IsNullOrEmpty() == true && scope == SettingScopeEnumeration.Tenant)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "cannot update tenant setting in host.");
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
            // bug: 这里挂载事件会导致生命周期延长, 先同步执行
            await _redisClient.SetNamedAsync(setting.GetRedisKey());
            //_dbContext.OnCommitted += async (sender) =>
            // {
            //     await _redisClient.SetNamedAsync(setting.GetRedisKey());
            // };
            return setting;
        }

        public async Task<IEnumerable<Setting>> GetActiveSettings()
        {
            var globalSettings = await this.GetGlobalSettings();
            var tenantSettings = await this.GetTenantSettings();
            var userSettings = await this.GetUserSettings();

            return userSettings
                .Union(tenantSettings, new GenericEqualityComparer<Setting>().With(x => x.Name))
                .Union(globalSettings, new GenericEqualityComparer<Setting>().With(x => x.Name))
                ;
        }
        public async Task<List<Setting>> GetGlobalSettings()
        {
            var globalSettings = await _redisClient.GetAllNamedByKeyAsync<Setting>($"{SettingScopeEnumeration.Global}:*");
            IEnumerable<Setting> result;
            if (SettingDefinitions.Except(globalSettings.Select(x => x.Value.Name)).Any())
            {
                var dbSettings = _dbContext.Query<Setting>().Where(x => x.Scope == SettingScopeEnumeration.Global).ToList();
                await TrySyncSettings(globalSettings, dbSettings);
                result = dbSettings;
            }
            else
            {
                result = globalSettings.Values;
            }
            return result.Union(SettingDefaults.Where(x => x.Scope == SettingScopeEnumeration.Global), new GenericEqualityComparer<Setting>().With(x => x.Name)).ToList();
        }

        private async Task TrySyncSettings(IDictionary<string, Setting> cachedSettings, List<Setting> dbSettings)
        {
            if (cachedSettings.Values.OrderBy(x => x.Name).SequenceEqual(dbSettings.OrderBy(x => x.Name), new GenericEqualityComparer<Setting>().With(x => x.Name)))
            {
                return;
            }
            await _redisClient.RemoveAllAsync(cachedSettings.Keys);
            _ = await _redisClient.AddAllAsync(dbSettings.Select(x => new Tuple<string, Setting>(x.GetRedisKey(), x)).ToList());
        }

        public async Task<List<Setting>> GetTenantSettings()
        {
            var tenantCode = _currentTenant?.Value?.Code;
            if (tenantCode.IsNullOrEmpty())
            {
                return new List<Setting>();
            }
            IEnumerable<Setting> result;
            var tenantSettings = await _redisClient.GetAllNamedByKeyAsync<Setting>($"{SettingScopeEnumeration.Tenant}:{tenantCode}:*");
            if (SettingDefinitions.Except(tenantSettings.Select(x => x.Value.Name)).Any())
            {
                var dbSettings = _dbContext.Query<Setting>().Where(x => x.Scope == SettingScopeEnumeration.Tenant && x.ScopedKey == tenantCode).ToList();
                await TrySyncSettings(tenantSettings, dbSettings);
                result = dbSettings;
            }
            else
            {
                result = tenantSettings.Values;
            }
            return result.Union(SettingDefaults.Where(x => x.Scope == SettingScopeEnumeration.Tenant), new GenericEqualityComparer<Setting>().With(x => x.Name)).ToList();
        }

        public async Task<List<Setting>> GetUserSettings()
        {
            var identity = _principal.Value;
            if (identity == null || identity.FindUserId().IsNullOrEmpty())
            {
                return new List<Setting>();
            }
            IEnumerable<Setting> result;
            var userSettings = await _redisClient.GetAllNamedByKeyAsync<Setting>($"{SettingScopeEnumeration.User}:{identity.FindUserId()}:*");
            if (SettingDefinitions.Except(userSettings.Select(x => x.Value.Name)).Any())
            {
                var dbSettings = _dbContext.Query<Setting>().Where(x => x.Scope == SettingScopeEnumeration.User && x.ScopedKey == identity.FindUserId()).ToList();
                await TrySyncSettings(userSettings, dbSettings);
                result = dbSettings;
            }
            else
            {
                result = userSettings.Values;
            }
            return result.Union(SettingDefaults.Where(x => x.Scope == SettingScopeEnumeration.User), new GenericEqualityComparer<Setting>().With(x => x.Name)).ToList();
        }

        public async Task<Setting?> GetOrNullAsync(SettingDefinition settingDefinition, SettingScopeEnumeration settingScope = default,
            string? scopedKey = default)
        {
            return await _redisClient.GetNamedAsync<Setting>(new Setting(settingDefinition, default, settingScope, scopedKey).GetRedisKey());
        }

        public virtual async Task<ISetting> Handle(EditSettingRequest request, CancellationToken cancellationToken)
        {
            return await SetAsync(request.Name, request.Scope, request.ScopedKey, request.Value);
        }

        public virtual async Task<IQueryable<ISetting>> Handle(GetSettingsRequest request, CancellationToken cancellationToken)
        {
            IEnumerable<Setting> settingValues = Enumerable.Empty<Setting>();
            if (request.Scope != default)
            {
                await request.Scope.SwitchAsync(
                    (SettingScopeEnumeration.User, async () => settingValues = await this.GetUserSettings()),
                    (SettingScopeEnumeration.Global, async () => settingValues = await this.GetGlobalSettings())
                );
            }
            else
            {
                settingValues = await this.GetActiveSettings();
            }
            settingValues = settingValues.WhereIf(!request.SettingDefinitions.IsNullOrEmpty(), x => request.SettingDefinitions.Contains(x.Name));
            settingValues = settingValues.WhereIf(!request.FilterByName.IsNullOrEmpty(), x => x.Name.Name.Contains(request.FilterByName, StringComparison.InvariantCultureIgnoreCase));
            var result = settingValues/*.Join(settingDefinitions, setting => setting.Name, settingDefinition => settingDefinition.Name, (settingValue, _) => settingValue)*/;
            return result.AsQueryable();
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<List<ISetting>> Handle(GetInitSettingsRequest request, CancellationToken cancellationToken)
        {
            var settingValues = await this.GetActiveSettings();
            return settingValues.Cast<ISetting>().ToList();
        }
    }
}
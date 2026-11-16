using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Geex.Extensions.Settings;
using Geex.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace Geex.Extensions.ApprovalFlows.Gql;

public partial class ApprovalFlowMutation
{
    public async Task<bool> SaveFlowConfig(string value)
    {
        JsonObject config;
        try
        {
            config = JsonNode.Parse(value) as JsonObject
                ?? throw new JsonException();
            foreach (var entry in config)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value is not JsonObject module
                    || module["useFlow"] is not JsonValue useFlow || !useFlow.TryGetValue<bool>(out _)
                    || (module["defaultTag"] != null
                        && (module["defaultTag"] is not JsonValue tag || !tag.TryGetValue<string>(out _))))
                    throw new JsonException();
            }
        }
        catch (JsonException)
        {
            throw new UserFriendlyException("流程配置须为模块对象, useFlow 为布尔值, defaultTag 为文本或空值.");
        }

        var tenant = _uow.ServiceProvider.GetService<ICurrentTenant>()?.Code;
        var scopeKey = string.IsNullOrEmpty(tenant) ? null : tenant;
        await _uow.ServiceProvider.GetRequiredService<ISettingService>().SetAsync(
            ApprovalFlowSettings.FlowSubmitConfig,
            scopeKey == null ? SettingScopeEnumeration.Global : SettingScopeEnumeration.Tenant,
            scopeKey, config);
        return true;
    }
}

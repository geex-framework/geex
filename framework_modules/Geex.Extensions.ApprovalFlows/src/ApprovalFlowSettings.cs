using System.Text.Json.Nodes;
using Geex.Extensions.Settings;

namespace Geex.Extensions.ApprovalFlows;

public class ApprovalFlowSettings : SettingDefinition
{
    public ApprovalFlowSettings(string name, JsonNode defaultValue)
        : base("ApprovalFlows" + name,
            [SettingScopeEnumeration.Global, SettingScopeEnumeration.Tenant], "流程分类配置", false, defaultValue) { }

    public static ApprovalFlowSettings Tags { get; } = new(nameof(Tags), new JsonArray());
}

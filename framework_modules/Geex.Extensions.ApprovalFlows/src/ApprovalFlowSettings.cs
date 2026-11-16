using System.Text.Json.Nodes;
using Geex.Extensions.Settings;

namespace Geex.Extensions.ApprovalFlows;

public class ApprovalFlowSettings : SettingDefinition
{
    public ApprovalFlowSettings(string name, JsonNode defaultValue)
        : base("ApprovalFlows" + name,
            [SettingScopeEnumeration.Global, SettingScopeEnumeration.Tenant], "流程分类配置", false, defaultValue) { }

    private ApprovalFlowSettings(string name, string description, JsonNode defaultValue)
        : base(name, [SettingScopeEnumeration.Global, SettingScopeEnumeration.Tenant], description, false, defaultValue) { }

    public static ApprovalFlowSettings Tags { get; } = new(nameof(Tags), new JsonArray());

    public static ApprovalFlowSettings FlowSubmitConfig { get; } = new(nameof(FlowSubmitConfig), "流程上报配置", new JsonObject());
}

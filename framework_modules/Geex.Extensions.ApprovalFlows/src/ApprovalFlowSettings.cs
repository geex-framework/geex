using System.Text.Json.Nodes;
using Geex.Extensions.Settings;

namespace Geex.Extensions.ApprovalFlows;

public class ApprovalFlowSettings : SettingDefinition
{
    public ApprovalFlowSettings(string name, JsonNode defaultValue) : this(name, defaultValue, "流程分类配置") { }

    private ApprovalFlowSettings(string name, JsonNode defaultValue, string description)
        : base("ApprovalFlows" + name,
            [SettingScopeEnumeration.Global, SettingScopeEnumeration.Tenant], description, false, defaultValue) { }

    public static ApprovalFlowSettings Tags { get; } = new(nameof(Tags), new JsonArray());

    public static ApprovalFlowSettings FlowSubmitConfig { get; } = new(nameof(FlowSubmitConfig), new JsonObject(), "流程上报配置");
}

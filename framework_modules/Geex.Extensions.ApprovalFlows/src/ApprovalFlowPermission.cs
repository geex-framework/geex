namespace Geex.Extensions.ApprovalFlows;

public class ApprovalFlowPermission : AppPermission<ApprovalFlowPermission>
{
    public ApprovalFlowPermission(string value) : base($"approvalFlows_{value}") { }

    internal ApprovalFlowPermission(string name, string value) : base(name, value) { }

    public static ApprovalFlowPermission QueryFlowConfig { get; } = new("query_flowConfig");
    public static ApprovalFlowPermission SaveFlowConfig { get; } = new("mutation_saveFlowConfig");
}

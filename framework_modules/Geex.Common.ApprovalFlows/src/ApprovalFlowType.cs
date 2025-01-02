using Geex.Common.Abstractions;

namespace Geex.Common.ApprovalFlows;

public class ApprovalFlowType : Enumeration<ApprovalFlowType>
{
    public static ApprovalFlowType ProjectSimulationSubmission { get; } = new ApprovalFlowType(nameof(ProjectSimulationSubmission), nameof(ProjectSimulationSubmission));

    public static ApprovalFlowType ProjectCreation { get; } = new ApprovalFlowType(nameof(ProjectCreation), nameof(ProjectCreation));

    public static ApprovalFlowType BatchProjectSimulationSubmission { get; } = new ApprovalFlowType(nameof(BatchProjectSimulationSubmission), nameof(BatchProjectSimulationSubmission));

    public ApprovalFlowType(string name, string value) : base(name, value)
    {
    }
}
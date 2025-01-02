using System.Collections.Immutable;

namespace Geex.Common.ApprovalFlows;

public class ApprovalFlowNodeTemplateData
{
    public string Id { get; set; }
    public string AuditRole { get; set; }
    public string Name { get; set; }
    public int Index { get; set; }
    public ImmutableList<string> CarbonCopyUserIds { get; set; }
}
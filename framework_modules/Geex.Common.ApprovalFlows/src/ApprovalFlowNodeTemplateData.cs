using System.Collections.Generic;
using System.Collections.Immutable;

namespace Geex.Common.ApprovalFlows;

public class ApprovalFlowNodeTemplateData:IApprovalFlowNodeTemplateData
{
    public string Id { get; set; }
    public string AuditRole { get; set; }
    public string Name { get; set; }

    /// <inheritdoc />
    public int? Index { get; set; }
    public List<string> CarbonCopyUserIds { get; set; } = new List<string>();
}
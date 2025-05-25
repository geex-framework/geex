using System.Collections.Generic;
using System.Collections.Immutable;
using Geex.Extensions.ApprovalFlows.Core.Entities;

namespace Geex.Extensions.ApprovalFlows;

public class ApprovalFlowNodeTemplateData:IApprovalFlowNodeTemplateData
{
    public string Id { get; set; }
    public string AuditRole { get; set; }
    public string Name { get; set; }

    /// <inheritdoc />
    public int? Index { get; set; }
    public List<string> CarbonCopyUserIds { get; set; } = new List<string>();

    /// <inheritdoc />
    public AssociatedEntityType? AssociatedEntityType { get; set; }
}

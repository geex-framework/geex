using System.Collections.Generic;
using System.Collections.Immutable;

namespace Geex.Common.ApprovalFlows.Requests;

public record ApprovalFlowNodeData : IApprovalFlowNodeData
{
    /// <inheritdoc />
    public bool? IsFromTemplate { get; set; }

    /// <inheritdoc />
    public string AuditRole { get; set; }

    /// <inheritdoc />
    public string? AuditUserId { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public string ApprovalFlowId { get; set; }

    /// <inheritdoc />
    public List<string> CarbonCopyUserIds { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public int? Index { get; set; } = 0;
}
using System.Collections.Generic;

namespace Geex.Extensions.ApprovalFlows;

public record ApprovalFlowNodeData : IApprovalFlowNodeData
{
    /// <inheritdoc />
    public string? Id { get; set; }

    /// <inheritdoc />
    public bool? IsFromTemplate { get; set; }

    /// <inheritdoc />
    public string? AuditRole { get; set; }

    /// <inheritdoc />
    public string? AuditUserId { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public string? ApprovalFlowId { get; set; }

    /// <inheritdoc />
    public List<string>? CarbonCopyUserIds { get; set; } = new List<string>();

    /// <inheritdoc />
    public string? Name { get; set; }

    /// <inheritdoc />
    public int? Index { get; set; } = 0;
}

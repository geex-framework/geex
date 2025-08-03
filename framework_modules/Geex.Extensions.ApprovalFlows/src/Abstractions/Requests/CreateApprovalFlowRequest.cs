
using System.Collections.Generic;
using System.Linq;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using MediatX;

namespace Geex.Extensions.ApprovalFlows.Requests;

public record CreateApprovalFlowRequest : IRequest<ApprovalFlow>, IApprovalFlowDate
{
    public string Name { get; set; }
    public string? Description { get; set; }

    /// <inheritdoc />
    List<IApprovalFlowNodeData> IApprovalFlowDate.Nodes => this.Nodes.Cast<IApprovalFlowNodeData>().ToList();
    public List<ApprovalFlowNodeData> Nodes { get; set; }
    /// <inheritdoc />

    public string OrgCode { get; set; }
    public string? TemplateId { get; set; }
    public AssociatedEntityType? AssociatedEntityType { get; set; }
    public string? AssociatedEntityId { get; set; }
}
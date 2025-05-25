
using System.Collections.Generic;
using System.Collections.Immutable;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using MediatX;

namespace Geex.Extensions.ApprovalFlows.Requests;

public record EditApprovalFlowRequest : IRequest<ApprovalFlow>
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<ApprovalFlowNodeData> Nodes { get; set; }
    public AssociatedEntityType? AssociatedEntityType { get; set; }
    public string? AssociatedEntityId { get; set; }
}
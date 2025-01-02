
using System.Collections.Immutable;
using MediatX;

namespace Geex.Common.ApprovalFlows.Requests
{
    public record EditApprovalFlowRequest : IRequest<ApprovalFlow>
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ImmutableList<ApprovalFlowNodeData> ApprovalFlowNodes { get; set; }
    }
}

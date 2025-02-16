
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using MediatX;

namespace Geex.Common.ApprovalFlows.Requests
{
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
}

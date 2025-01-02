
using System;
using System.Linq;
using MediatX;

namespace Geex.Common.ApprovalFlows.Requests
{
    public record QueryApprovalFlowRequest : IRequest<IQueryable<ApprovalFlow>>
    {
        public string? TemplateId { get; set; }
        public string? CreatorUserId { get; set; }
        public ApprovalFlowType? Type { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
    }

    public record QueryApprovalFlowTemplateRequest : IRequest<IQueryable<ApprovalFlowTemplate>>
    {
        public string? CreatorUserId { get; set; }
        public ApprovalFlowType? Type { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
    }
}

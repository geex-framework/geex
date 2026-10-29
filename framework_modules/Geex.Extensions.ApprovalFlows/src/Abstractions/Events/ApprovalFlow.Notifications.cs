using Geex.Extensions.ApprovalFlows.Core.Entities;
using MediatX;

namespace Geex.Extensions.ApprovalFlows.Events
{
    public class ApprovalFlowFinishEvent : IEvent
    {
        public ApprovalFlow ApprovalFlow { get; }
        public string Id { get; }

        public ApprovalFlowFinishEvent(ApprovalFlow approvalflow, string id)
        {
            ApprovalFlow = approvalflow;
            Id = id;
        }
    }

    public class ApprovalFlowClosedEvent : IEvent
    {
        public ApprovalFlow ApprovalFlow { get; }
        public string Id { get; }

        public ApprovalFlowClosedEvent(ApprovalFlow approvalflow, string id)
        {
            ApprovalFlow = approvalflow;
            Id = id;
        }
    }
}

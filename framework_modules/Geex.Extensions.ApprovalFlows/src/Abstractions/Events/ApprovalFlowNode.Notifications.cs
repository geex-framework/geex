using System.Collections.Generic;
using System.Linq;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using MediatX;

namespace Geex.Extensions.ApprovalFlows.Events;

public abstract class ApprovalFlowNodeStatusChangeEvent : INotification
{
    public ApprovalFlowNodeStatusChangeEvent(ApprovalFlowNode approvalflowNode)
    {
        this.ApprovalFlowId = approvalflowNode.ApprovalFlowId;
        this.ApprovalFlowNodeId = approvalflowNode.Id;
    }

    public string ApprovalFlowNodeId { get; set; }

    public string ApprovalFlowId { get; set; }
}
public class ApprovalFlowNodeStartEvent : ApprovalFlowNodeStatusChangeEvent
{
    public ApprovalFlowNodeStartEvent(ApprovalFlowNode approvalflowNode) : base(approvalflowNode)
    {
        ApprovalFlowId = approvalflowNode.ApprovalFlowId;
        AuditUserId = approvalflowNode.AuditUserId;
    }

    public string AuditUserId { get; set; }
}
public class ApprovalFlowNodeApprovedEvent : ApprovalFlowNodeStatusChangeEvent
{
    public ApprovalFlowNodeApprovedEvent(ApprovalFlowNode approvalflowNode) : base(approvalflowNode)
    {
    }
}

public class ApprovalFlowNodeRejectedEvent : ApprovalFlowNodeStatusChangeEvent
{
    public ApprovalFlowNodeRejectedEvent(ApprovalFlowNode approvalflowNode) : base(approvalflowNode)
    {
    }
}
public class ApprovalFlowNodeTransferredEvent : ApprovalFlowNodeStatusChangeEvent
{
    public ApprovalFlowNodeTransferredEvent(ApprovalFlowNode approvalflowNode, string originUserId, string newUserId) : base(approvalflowNode)
    {
        this.OriginUserId = originUserId;
        NewUserId = newUserId;
    }

    public string OriginUserId { get; set; }
    public string NewUserId { get; }
}

public class ApprovalFlowNodeBulkRejectedEvent : ApprovalFlowNodeStatusChangeEvent
{
    public List<ApprovalFlowNode> NodesToReject { get; }
    public string TargetNodeId { get; }

    public ApprovalFlowNodeBulkRejectedEvent(ApprovalFlowNode approvalflowNode, string targetNodeId) : base(approvalflowNode)
    {
        TargetNodeId = targetNodeId;
    }

    public ApprovalFlowNodeBulkRejectedEvent(ApprovalFlowNode approvalflowNode, List<ApprovalFlowNode> nodesToReject) : base(approvalflowNode)
    {
        NodesToReject = nodesToReject.ToList();
    }
}

public class ApprovalFlowNodeConsultRepliedEvent : ApprovalFlowNodeStatusChangeEvent
{
    public string ReplyUserId { get; }

    public ApprovalFlowNodeConsultRepliedEvent(ApprovalFlowNode approvalflowNode, string replyUserId) : base(approvalflowNode)
    {
        ReplyUserId = replyUserId;
    }
}
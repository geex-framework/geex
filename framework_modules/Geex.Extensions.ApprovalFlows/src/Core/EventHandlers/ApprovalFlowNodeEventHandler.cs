using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using Geex.Extensions.ApprovalFlows.Events;
using Geex.Extensions.Identity;
using Geex.Extensions.Messaging;
using Geex.Extensions.Messaging.Requests;
using MediatX;
using Volo.Abp.DependencyInjection;

namespace Geex.Extensions.ApprovalFlows.Core.EventHandlers;

public class ApprovalFlowNodeEventHandler : IEventHandler<ApprovalFlowNodeStartEvent>, IEventHandler<ApprovalFlowNodeConsultRepliedEvent>, IEventHandler<ApprovalFlowNodeApprovedEvent>, IEventHandler<ApprovalFlowNodeTransferredEvent>, IEventHandler<ApprovalFlowNodeRejectedEvent>, IEventHandler<ApprovalFlowNodeBulkRejectedEvent>, ITransientDependency
{
    private readonly IUnitOfWork _uow;

    public ApprovalFlowNodeEventHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }


    public async Task Handle(ApprovalFlowNodeApprovedEvent eventData, CancellationToken cancellationToken)
    {
        var node = _uow.Query<ApprovalFlowNode>().GetById(eventData.ApprovalFlowNodeId);
        var flow = node.ApprovalFlow.Value;
        var userIdsToNotify = ResolveStakeholderNotifyUserIds(node, flow);
        if (!userIdsToNotify.Any())
        {
            return;
        }

        var messageEntity = await _uow.Request(new CreateMessageRequest()
        {
            Severity = MessageSeverityType.Success,
            Text = $"【工作流】:{flow.Name} 的审批已通过.",
            Meta = new JsonObject([new("ApprovalFlowId", node.ApprovalFlowId)]),
        });
        await _uow.Request(new SendNotificationMessageRequest()
        {
            MessageId = messageEntity.Id,
            ToUserIds = [.. userIdsToNotify]
        });
    }

    public async Task Handle(ApprovalFlowNodeRejectedEvent eventData, CancellationToken cancellationToken)
    {
        var node = _uow.Query<ApprovalFlowNode>().GetById(eventData.ApprovalFlowNodeId)
            ?? throw new BusinessException(GeexExceptionType.OnPurpose, message: "Approval flow node not found.");
        var flow = _uow.Query<ApprovalFlow>().GetById(eventData.ApprovalFlowId);
        var flowName = flow?.Name ?? "工作流";
        var userIdsToNotify = ResolveStakeholderNotifyUserIds(node, flow);
        if (!userIdsToNotify.Any())
        {
            return;
        }

        var messageEntity = await _uow.Request(new CreateMessageRequest()
        {
            Severity = MessageSeverityType.Warn,
            Text = $"【工作流】:{flowName} 的审批被驳回.",
            Meta = new JsonObject([new("ApprovalFlowId", node.ApprovalFlowId)]),
        });
        await _uow.Request(new SendNotificationMessageRequest()
        {
            MessageId = messageEntity.Id,
            ToUserIds = [.. userIdsToNotify]
        });
    }

    public async Task Handle(ApprovalFlowNodeTransferredEvent eventData, CancellationToken cancellationToken)
    {
        var node = _uow.Query<ApprovalFlowNode>().GetById(eventData.ApprovalFlowNodeId);
        var originUserName = _uow.Query<IUser>().GetById(eventData.OriginUserId).Nickname;
        var messageEntity = await _uow.Request(new CreateMessageRequest()
        {
            Severity = MessageSeverityType.Warn,
            Text = $"【工作流】:{node.ApprovalFlow.Value.Name} 的审批权限已由 {originUserName} 移交给您.",
            Meta = new JsonObject([new("ApprovalFlowId", node.ApprovalFlowId)]),
        });
        await _uow.Request(new SendNotificationMessageRequest()
        {
            MessageId = messageEntity.Id,
            ToUserIds = [eventData.NewUserId]
        });
    }

    public async Task Handle(ApprovalFlowNodeBulkRejectedEvent eventData, CancellationToken cancellationToken)
    {
        foreach (var node in eventData.NodesToReject)
        {
            var flow = node.ApprovalFlow.Value;
            var userIdsToNotify = ResolveStakeholderNotifyUserIds(node, flow);
            if (!userIdsToNotify.Any())
            {
                continue;
            }

            var messageEntity = await _uow.Request(new CreateMessageRequest()
            {
                Severity = MessageSeverityType.Warn,
                Text = $"【工作流】:{flow.Name} 的审批被驳回.",
                Meta = new JsonObject([new("ApprovalFlowId", node.ApprovalFlowId)]),
            });
            await _uow.Request(new SendNotificationMessageRequest()
            {
                MessageId = messageEntity.Id,
                ToUserIds = [.. userIdsToNotify]
            });
        }
    }

    public async Task Handle(ApprovalFlowNodeStartEvent eventData, CancellationToken cancellationToken)
    {
        var node = _uow.Query<ApprovalFlowNode>().GetById(eventData.ApprovalFlowNodeId);
        var messageEntity = await _uow.Request(new CreateMessageRequest()
        {
            Severity = MessageSeverityType.Warn,
            Text = $"【工作流】:{node.ApprovalFlow.Value.Name} 需要您进行审批, 请尽快处理.",
            Meta = new JsonObject([new("ApprovalFlowId", node.ApprovalFlowId)]),
        });
        await _uow.Request(new SendNotificationMessageRequest()
        {
            MessageId = messageEntity.Id,
            ToUserIds = [node.AuditUserId]
        });
    }

    public async Task Handle(ApprovalFlowNodeConsultRepliedEvent eventData, CancellationToken cancellationToken)
    {
        var node =  _uow.Query<ApprovalFlowNode>().GetById(eventData.ApprovalFlowNodeId);
        var messageEntity = await _uow.Request(new CreateMessageRequest()
        {
            Severity = MessageSeverityType.Warn,
            Text = $"【工作流】:{node.ApprovalFlow.Value.Name} 的征询意见已回复, 请确认.",
            Meta = new JsonObject([new("ApprovalFlowId", node.ApprovalFlowId)]),
        });
        await _uow.Request(new SendNotificationMessageRequest()
        {
            MessageId = messageEntity.Id,
            ToUserIds = [node.AuditUserId]
        });
    }

    /// <summary>
    /// Carbon-copy users + flow creator, excluding the node auditor who just acted.
    /// </summary>
    private static IEnumerable<string> ResolveStakeholderNotifyUserIds(ApprovalFlowNode node, ApprovalFlow? flow)
    {
        var excludedAuditorId = node.AuditUserId;
        return node.CarbonCopyUserIds
            .Append(flow?.CreatorUserId)
            .Where(x => !x.IsNullOrEmpty() && x != excludedAuditorId)
            .Distinct()!;
    }
}

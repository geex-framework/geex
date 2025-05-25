using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Geex.Abstractions.Entities;
using KuanFang.Rms.MessageManagement.Messages;
using Geex.Common.Requests.Messaging;

using MediatR;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.DependencyInjection;
using Geex.Abstractions;

namespace Geex.Common.ApprovalFlows.EventHandlers
{
    public class NotifyUsersOnNodeStatusChanged : INotificationHandler<ApprovalFlowNodeStartEvent>, INotificationHandler<ApprovalFlowNodeConsultRepliedEvent>, INotificationHandler<ApprovalFlowNodeApprovedEvent>, INotificationHandler<ApprovalFlowNodeTransferredEvent>, INotificationHandler<ApprovalFlowNodeRejectedEvent>, INotificationHandler<ApprovalFlowNodeBulkRejectedEvent>, ITransientDependency
    {
        private readonly IUnitOfWork _uow;

        public NotifyUsersOnNodeStatusChanged(IUnitOfWork uow)
        {
            _uow = uow;
        }


        public async Task Handle(ApprovalFlowNodeApprovedEvent eventData, CancellationToken cancellationToken)
        {
            var node = _uow.Query<ApprovalFlowNode>().GetById(eventData.ApprovalFlowNodeId);
            var userIdsToNotify = _uow.Query<IUser>()
                .Where(x => node.CarbonCopyUserIds.Contains(x.Id)).Select(x => x.Id).AsEnumerable();
            userIdsToNotify = userIdsToNotify.Concat(new[] { node.AuditUserId });
            var messageEntity = await _uow.Request(new CreateMessageRequest()
            {
                Severity = MessageSeverityType.Success,
                Text = $"【工作流】:{node.ApprovalFlow.Value.Name} 的审批已通过.",
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
                var userIdsToNotify = _uow.Query<IUser>()
                    .Where(x => node.CarbonCopyUserIds.Contains(x.Id)).Select(x => x.Id).AsEnumerable();
                userIdsToNotify = userIdsToNotify.Concat(new[] { node.AuditUserId });

                var messageEntity = await _uow.Request(new CreateMessageRequest()
                {
                    Severity = MessageSeverityType.Warn,
                    Text = $"【工作流】:{node.ApprovalFlow.Value.Name} 的审批被驳回.",
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
    }
}

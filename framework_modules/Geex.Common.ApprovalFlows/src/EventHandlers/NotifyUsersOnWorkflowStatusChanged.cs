using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Common.Requests.Messaging;


using KuanFang.Rms.MessageManagement.Messages;

using MediatR;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.DependencyInjection;

namespace Geex.Common.ApprovalFlows.EventHandlers
{
    public class NotifyUsersOnApprovalFlowStatusChanged : INotificationHandler<ApprovalFlowFinishEvent>, ITransientDependency
    {
        private readonly IUnitOfWork _uow;

        public NotifyUsersOnApprovalFlowStatusChanged(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task Handle(ApprovalFlowFinishEvent eventData, CancellationToken cancellationToken)
        {
            var messageEntity = await _uow.Request(new CreateMessageRequest()
            {
                Severity = MessageSeverityType.Success,
                Text = $"【工作流】:{eventData.ApprovalFlow.Name} 的已经审批完成.",
                Meta = new JsonObject([new("ApprovalFlowId", eventData.ApprovalFlow.Id)]),
            });
            await _uow.Request(new SendNotificationMessageRequest()
            {
                MessageId = messageEntity.Id,
                ToUserIds = [eventData.ApprovalFlow.CreatorUserId]
            });
        }
    }
}

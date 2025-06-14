using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Extensions.ApprovalFlows.Events;
using Geex.Extensions.Messaging;
using Geex.Extensions.Messaging.Requests;
using MediatX;
using Volo.Abp.DependencyInjection;

namespace Geex.Extensions.ApprovalFlows.Core.EventHandlers;

public class ApprovalFlowEventHandler : IEventHandler<ApprovalFlowFinishEvent>, ITransientDependency
{
    private readonly IUnitOfWork _uow;

    public ApprovalFlowEventHandler(IUnitOfWork uow)
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
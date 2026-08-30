using Geex.Extensions.Messaging;
using Geex.Extensions.Messaging.Requests;
using Geex.Extensions.ApprovalFlows.Events;
using MediatX;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Geex.Extensions.ApprovalFlows.Core.EventHandlers;

public class ApprovalFlowEventHandler :
    IEventHandler<ApprovalFlowFinishEvent>,
    IEventHandler<ApprovalFlowCanceledEvent>,
    ITransientDependency
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

    public async Task Handle(ApprovalFlowCanceledEvent eventData, CancellationToken cancellationToken)
    {
        var messageEntity = await _uow.Request(new CreateMessageRequest()
        {
            Severity = MessageSeverityType.Warn,
            Text = $"【工作流】:{eventData.ApprovalFlow.Name} 已取消.",
            Meta = new JsonObject([new("ApprovalFlowId", eventData.ApprovalFlow.Id)]),
        });
        var toUserIds = eventData.ApprovalFlow.Stakeholders
            .Select(x => x.UserId)
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .ToList();
        if (toUserIds.Count == 0 && !string.IsNullOrEmpty(eventData.ApprovalFlow.CreatorUserId))
        {
            toUserIds.Add(eventData.ApprovalFlow.CreatorUserId!);
        }
        if (toUserIds.Count == 0)
        {
            return;
        }
        await _uow.Request(new SendNotificationMessageRequest()
        {
            MessageId = messageEntity.Id,
            ToUserIds = [.. toUserIds]
        });
    }
}

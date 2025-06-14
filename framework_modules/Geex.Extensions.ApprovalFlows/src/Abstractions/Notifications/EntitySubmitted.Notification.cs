using MediatX;

namespace Geex.Extensions.ApprovalFlows.Notifications;

public class EntitySubmittedEvent<TEntity> : IEvent
{
    public IApproveEntity Entity { get; }

    public EntitySubmittedEvent(IApproveEntity entity)
    {
        Entity = entity;
    }
}
using MediatX;

namespace Geex.Extensions.ApprovalFlows.Events;

public class EntityUnSubmittedEvent<TEntity> : IEvent
{
    public IApproveEntity Entity { get; }

    public EntityUnSubmittedEvent(IApproveEntity entity)
    {
        Entity = entity;
    }
}

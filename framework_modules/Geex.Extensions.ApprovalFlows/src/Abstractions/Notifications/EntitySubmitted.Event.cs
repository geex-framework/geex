using MediatX;

namespace Geex.Extensions.ApprovalFlows.Events;

public class EntitySubmittedEvent<TEntity> : IEvent
{
    public IApproveEntity Entity { get; }

    public EntitySubmittedEvent(IApproveEntity entity)
    {
        Entity = entity;
    }
}

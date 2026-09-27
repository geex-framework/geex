using MediatX;

namespace Geex.Extensions.ApprovalFlows.Events;

public class EntityUnApprovedEvent<TEntity> : IEvent
{
    public IApproveEntity Entity { get; }

    public EntityUnApprovedEvent(IApproveEntity entity)
    {
        Entity = entity;
    }
}

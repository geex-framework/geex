using MediatX;

namespace Geex.Extensions.ApprovalFlows.Events;

public class EntityApprovedEvent<TEntity> : IEvent
{
    public IApproveEntity Entity { get; }

    public EntityApprovedEvent(IApproveEntity entity)
    {
        Entity = entity;
    }
}

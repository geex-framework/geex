using MediatX;

namespace Geex.Extensions.ApprovalFlows.Notifications;

public class EntityUnSubmittedNotification<TEntity> : IEvent
{
    public IApproveEntity Entity { get; }

    public EntityUnSubmittedNotification(IApproveEntity entity)
    {
        Entity = entity;
    }
}
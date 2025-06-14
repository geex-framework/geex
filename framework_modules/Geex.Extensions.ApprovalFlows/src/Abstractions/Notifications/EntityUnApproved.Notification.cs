using MediatX;

namespace Geex.Extensions.ApprovalFlows.Notifications;

public class EntityUnApprovedNotification<TEntity> : IEvent
{
    public IApproveEntity Entity { get; }

    public EntityUnApprovedNotification(IApproveEntity entity)
    {
        Entity = entity;
    }
}
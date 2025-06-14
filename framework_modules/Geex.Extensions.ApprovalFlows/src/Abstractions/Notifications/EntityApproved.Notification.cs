using MediatX;

namespace Geex.Extensions.ApprovalFlows.Notifications;

public class EntityApprovedNotification<TEntity> : IEvent
{
    public IApproveEntity Entity { get; }

    public EntityApprovedNotification(IApproveEntity entity)
    {
        Entity = entity;
    }
}
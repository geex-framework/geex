using MediatR;

namespace Geex.Extensions.ApprovalFlows.Notifications;

public class EntityUnApprovedNotification<TEntity> : INotification
{
    public IApproveEntity Entity { get; }

    public EntityUnApprovedNotification(IApproveEntity entity)
    {
        Entity = entity;
    }
}
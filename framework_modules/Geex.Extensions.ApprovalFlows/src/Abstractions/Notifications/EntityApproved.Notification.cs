using MediatR;

namespace Geex.Extensions.ApprovalFlows.Notifications;

public class EntityApprovedNotification<TEntity> : INotification
{
    public IApproveEntity Entity { get; }

    public EntityApprovedNotification(IApproveEntity entity)
    {
        Entity = entity;
    }
}
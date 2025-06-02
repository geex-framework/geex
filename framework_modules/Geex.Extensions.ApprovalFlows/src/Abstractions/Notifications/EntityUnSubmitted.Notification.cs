using MediatR;

namespace Geex.Extensions.ApprovalFlows.Notifications;

public class EntityUnSubmittedNotification<TEntity> : INotification
{
    public IApproveEntity Entity { get; }

    public EntityUnSubmittedNotification(IApproveEntity entity)
    {
        Entity = entity;
    }
}
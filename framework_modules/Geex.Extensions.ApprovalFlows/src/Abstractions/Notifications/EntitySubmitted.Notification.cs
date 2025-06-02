using MediatR;

namespace Geex.Extensions.ApprovalFlows.Notifications;

public class EntitySubmittedNotification<TEntity> : INotification
{
    public IApproveEntity Entity { get; }

    public EntitySubmittedNotification(IApproveEntity entity)
    {
        Entity = entity;
    }
}
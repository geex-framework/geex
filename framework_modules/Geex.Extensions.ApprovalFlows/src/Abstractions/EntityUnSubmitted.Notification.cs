using MediatR;

namespace Geex.ApprovalFlows;

public class EntityUnSubmittedNotification<TEntity> : INotification
{
    public IApproveEntity Entity { get; }

    public EntityUnSubmittedNotification(IApproveEntity entity)
    {
        Entity = entity;
    }
}
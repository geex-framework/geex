using MediatR;

namespace Geex.ApprovalFlows;

public class EntitySubmittedNotification<TEntity> : INotification
{
    public IApproveEntity Entity { get; }

    public EntitySubmittedNotification(IApproveEntity entity)
    {
        Entity = entity;
    }
}
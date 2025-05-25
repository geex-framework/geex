using MediatR;

namespace Geex.ApprovalFlows;

public class EntityUnApprovedNotification<TEntity> : INotification
{
    public IApproveEntity Entity { get; }

    public EntityUnApprovedNotification(IApproveEntity entity)
    {
        Entity = entity;
    }
}
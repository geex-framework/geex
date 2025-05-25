using MediatR;

namespace Geex.ApprovalFlows;

public class EntityApprovedNotification<TEntity> : INotification
{
    public IApproveEntity Entity { get; }

    public EntityApprovedNotification(IApproveEntity entity)
    {
        Entity = entity;
    }
}
using MediatR;

namespace Geex.Abstractions.Approval.Events
{
    public class EntityApprovedNotification<TEntity> : INotification
    {
        public IApproveEntity Entity { get; }

        public EntityApprovedNotification(IApproveEntity entity)
        {
            Entity = entity;
        }
    }
}

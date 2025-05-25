using MediatR;

namespace Geex.Abstractions.Approval.Events
{
    public class EntityUnApprovedNotification<TEntity> : INotification
    {
        public IApproveEntity Entity { get; }

        public EntityUnApprovedNotification(IApproveEntity entity)
        {
            Entity = entity;
        }
    }
}

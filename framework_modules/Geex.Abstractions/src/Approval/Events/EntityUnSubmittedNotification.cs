using MediatR;

namespace Geex.Abstractions.Approval.Events
{
    public class EntityUnSubmittedNotification<TEntity> : INotification
    {
        public IApproveEntity Entity { get; }

        public EntityUnSubmittedNotification(IApproveEntity entity)
        {
            Entity = entity;
        }
    }
}

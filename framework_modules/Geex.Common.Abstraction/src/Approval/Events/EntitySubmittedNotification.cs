using MediatR;

namespace Geex.Common.Abstraction.Approval.Events
{
    public class EntitySubmittedNotification<TEntity> : INotification
    {
        public IApproveEntity Entity { get; }

        public EntitySubmittedNotification(IApproveEntity entity)
        {
            Entity = entity;
        }
    }
}

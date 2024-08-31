using MediatR;

namespace Geex.Common.Abstraction.Approbation.Events
{
    public class EntityUnsubmittedNotification<TEntity> : INotification
    {
        public IApproveEntity Entity { get; }

        public EntityUnsubmittedNotification(IApproveEntity entity)
        {
            Entity = entity;
        }
    }
}

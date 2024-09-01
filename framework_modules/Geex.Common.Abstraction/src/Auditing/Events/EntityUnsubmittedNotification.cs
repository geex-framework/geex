using MediatR;

namespace Geex.Common.Abstraction.Approbation.Events
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

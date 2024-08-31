using MediatR;

namespace Geex.Common.Abstraction.Approbation.Events
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

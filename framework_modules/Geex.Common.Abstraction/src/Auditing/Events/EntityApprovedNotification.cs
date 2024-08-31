using MediatR;

namespace Geex.Common.Abstraction.Approbation.Events
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

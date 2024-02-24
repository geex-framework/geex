using MediatR;

namespace Geex.Common.Abstraction.Auditing.Events
{
    public class EntityAuditedNotification<TEntity> : INotification
    {
        public IAuditEntity Entity { get; }

        public EntityAuditedNotification(IAuditEntity entity)
        {
            Entity = entity;
        }
    }
}

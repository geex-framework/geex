using MediatR;

namespace Geex.Common.Abstraction.Auditing.Events
{
    public class EntityUnsubmittedNotification<TEntity> : INotification
    {
        public IAuditEntity Entity { get; }

        public EntityUnsubmittedNotification(IAuditEntity entity)
        {
            Entity = entity;
        }
    }
}

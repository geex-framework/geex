using MediatR;

namespace Geex.Common.Abstraction.Auditing.Events
{
    public class EntitySubmittedNotification<TEntity> : INotification
    {
        public IAuditEntity Entity { get; }

        public EntitySubmittedNotification(IAuditEntity entity)
        {
            Entity = entity;
        }
    }
}

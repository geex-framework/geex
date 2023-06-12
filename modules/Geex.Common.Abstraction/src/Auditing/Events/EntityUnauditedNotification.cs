using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;

namespace Geex.Common.Abstraction.Auditing.Events
{
    public class EntityUnauditedNotification<TEntity> : INotification
    {
        public IAuditEntity Entity { get; }

        public EntityUnauditedNotification(IAuditEntity entity)
        {
            Entity = entity;
        }
    }
}

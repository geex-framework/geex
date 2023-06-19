using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

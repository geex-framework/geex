﻿using MediatR;

namespace Geex.Common.Abstraction.Approbation.Events
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

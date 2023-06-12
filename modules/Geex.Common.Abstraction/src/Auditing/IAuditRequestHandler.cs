﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstractions;

using MediatR;

using MongoDB.Entities;

namespace Geex.Common.Abstraction.Auditing
{
    public interface IAuditRequestHandler<TInterface, TEntity> :
        ICommonHandler<TInterface, TEntity>,
        IRequestHandler<SubmitRequest<TInterface>>,
        IRequestHandler<AuditRequest<TInterface>>,
        IRequestHandler<UnsubmitRequest<TInterface>>,
        IRequestHandler<UnauditRequest<TInterface>>
        where TInterface : IAuditEntity where TEntity : TInterface
    {

        async Task<Unit> IRequestHandler<SubmitRequest<TInterface>, Unit>.Handle(SubmitRequest<TInterface> request, CancellationToken cancellationToken)
        {
            var entities = DbContext.Queryable<TEntity>().Where(x => request.Ids.Contains(x.Id)).ToList();
            if (!entities.Any())
            {
                throw new BusinessException(GeexExceptionType.NotFound);
            }
            foreach (var entity in entities)
            {
                if (entity is { DbContext: null })
                {
                    DbContext.Attach(entity);
                }
                await entity.Submit<TInterface>(request.Remark);
            }
            return Unit.Value;
        }

        async Task<Unit> IRequestHandler<AuditRequest<TInterface>, Unit>.Handle(AuditRequest<TInterface> request, CancellationToken cancellationToken)
        {
            var entities = DbContext.Queryable<TEntity>().Where(x => request.Ids.Contains(x.Id)).ToList();
            if (!entities.Any())
            {
                throw new BusinessException(GeexExceptionType.NotFound);
            }
            foreach (var entity in entities)
            {
                if (entity is { DbContext: null })
                {
                    DbContext.Attach(entity);
                }

                await entity.Audit<TInterface>(request.Remark);
            }

            return Unit.Value;
        }

        async Task<Unit> IRequestHandler<UnsubmitRequest<TInterface>, Unit>.Handle(UnsubmitRequest<TInterface> request, CancellationToken cancellationToken)
        {
            var entities = DbContext.Queryable<TEntity>().Where(x => request.Ids.Contains(x.Id)).ToList();
            if (!entities.Any())
            {
                throw new BusinessException(GeexExceptionType.NotFound);
            }
            foreach (var entity in entities)
            {
                if (entity is { DbContext: null })
                {
                    DbContext.Attach(entity);
                }
                await entity.UnSubmit<TInterface>(request.Remark);
            }
            return Unit.Value;
        }

        async Task<Unit> IRequestHandler<UnauditRequest<TInterface>, Unit>.Handle(UnauditRequest<TInterface> request, CancellationToken cancellationToken)
        {
            var entities = DbContext.Queryable<TEntity>().Where(x => request.Ids.Contains(x.Id)).ToList();
            if (!entities.Any())
            {
                throw new BusinessException(GeexExceptionType.NotFound);
            }
            foreach (var entity in entities)
            {
                if (entity is { DbContext: null })
                {
                    DbContext.Attach(entity);
                }

                await entity.UnAudit<TInterface>(request.Remark);
            }

            return Unit.Value;
        }
    }
}
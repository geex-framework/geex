﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Requests;
using Geex.Requests;
using MediatX;
using MongoDB.Entities;
using MongoDB.Entities.Utilities;

namespace Geex
{
    public interface ICommonHandler<TInterface, TEntity> :
        IRequestHandler<QueryRequest<TInterface>, IQueryable<TInterface>>,
        IRequestHandler<DeleteRequest<TInterface>, long>
        where TInterface : IEntityBase where TEntity : TInterface
    {
        public IUnitOfWork Uow { get; }

        async Task<IQueryable<TInterface>> MediatR.IRequestHandler<QueryRequest<TInterface>, IQueryable<TInterface>>.Handle(QueryRequest<TInterface> request, CancellationToken cancellationToken)
        {
            if (request.Filter != default)
            {
                return (IQueryable<TInterface>)Uow.Query<TEntity>().Where(request.Filter.CastParamType<TEntity>() as Expression<Func<TEntity, bool>>);
            }
            return (IQueryable<TInterface>)Uow.Query<TEntity>();
        }
        async Task<long> MediatR.IRequestHandler<DeleteRequest<TInterface>, long>.Handle(DeleteRequest<TInterface> request, CancellationToken cancellationToken)
        {
            if (request.Filter != default)
            {
                var deleteResult = await Uow.DbContext.DeleteAsync(request.Filter.CastParamType<TEntity>() as Expression<Func<TEntity, bool>>, cancellationToken);
                return deleteResult;
            }
            throw new InvalidOperationException("bulk deletion must specify a filter.");
        }
    }

    public interface ICommonHandler<TEntity> :
        IRequestHandler<QueryRequest<TEntity>, IQueryable<TEntity>>,
        IRequestHandler<DeleteRequest<TEntity>, long>
        where TEntity : IEntityBase
    {
        public IUnitOfWork Uow { get; }

        async Task<IQueryable<TEntity>> MediatR.IRequestHandler<QueryRequest<TEntity>, IQueryable<TEntity>>.Handle(QueryRequest<TEntity> request, CancellationToken cancellationToken)
        {
            if (request.Filter != default)
            {
                return Uow.Query<TEntity>().Where(request.Filter.CastParamType<TEntity>() as Expression<Func<TEntity, bool>>);
            }
            return Uow.Query<TEntity>();
        }
        async Task<long> MediatR.IRequestHandler<DeleteRequest<TEntity>, long>.Handle(DeleteRequest<TEntity> request, CancellationToken cancellationToken)
        {
            if (request.Filter != default)
            {
                var deleteResult = await Uow.DbContext.DeleteAsync(request.Filter.CastParamType<TEntity>() as Expression<Func<TEntity, bool>>, cancellationToken);
                return deleteResult;
            }
            throw new InvalidOperationException("bulk deletion must specify a filter.");
        }
    }
}

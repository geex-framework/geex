using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Requests;
using Geex.Common.Abstraction.Storage;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Entities;
using MongoDB.Entities.Utilities;

namespace Geex.Common.Abstraction.Entities
{
    public interface ICommonHandler<TInterface, TEntity> :
        IRequestHandler<QueryRequest<TInterface>, IQueryable<TInterface>>,
        IRequestHandler<DeleteRequest<TInterface>, long>
        where TInterface : IEntityBase where TEntity : TInterface
    {
        public IUnitOfWork Uow { get; }

        async Task<IQueryable<TInterface>> IRequestHandler<QueryRequest<TInterface>, IQueryable<TInterface>>.Handle(QueryRequest<TInterface> request, CancellationToken cancellationToken)
        {
            if (request.Filter != default)
            {
                return (IQueryable<TInterface>)Uow.Query<TEntity>().Where(request.Filter.CastParamType<TEntity>() as Expression<Func<TEntity, bool>>);
            }
            return (IQueryable<TInterface>)Uow.Query<TEntity>();
        }
        async Task<long> IRequestHandler<DeleteRequest<TInterface>, long>.Handle(DeleteRequest<TInterface> request, CancellationToken cancellationToken)
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

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Requests;
using MediatR;

using MongoDB.Entities;
using MongoDB.Entities.Utilities;

namespace Geex.Common.Abstraction.Entities
{
    public interface ICommonHandler<TInterface, TEntity> :
        IRequestHandler<QueryRequest<TInterface>, IQueryable<TInterface>>
        where TInterface : IEntityBase where TEntity : TInterface
    {
        public DbContext DbContext { get; }

        async Task<IQueryable<TInterface>> IRequestHandler<QueryRequest<TInterface>, IQueryable<TInterface>>.Handle(QueryRequest<TInterface> request, CancellationToken cancellationToken)
        {
            if (request.Filter != default)
            {
                return (IQueryable<TInterface>)DbContext.Query<TEntity>().Where(request.Filter.CastParamType<TEntity>() as Expression<Func<TEntity, bool>>);
            }
            return (IQueryable<TInterface>)DbContext.Query<TEntity>();
        }
    }
}

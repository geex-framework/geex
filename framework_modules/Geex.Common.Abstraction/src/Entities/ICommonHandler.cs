using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Abstractions;

using MediatR;

using MongoDB.Entities;
using MongoDB.Entities.Utilities;

namespace Geex.Common.Abstraction.Entities
{
    public interface ICommonHandler<TInterface, TEntity> :
        IRequestHandler<QueryInput<TInterface>, IQueryable<TInterface>>
        where TInterface : IEntityBase where TEntity : TInterface
    {
        public DbContext DbContext { get; }

        async Task<IQueryable<TInterface>> IRequestHandler<QueryInput<TInterface>, IQueryable<TInterface>>.Handle(QueryInput<TInterface> request, CancellationToken cancellationToken)
        {
            if (request.Filter != default)
            {
                return (IQueryable<TInterface>)DbContext.Query<TEntity>().Where(request.Filter.CastParamType<TEntity>() as Expression<Func<TEntity, bool>>);
            }
            return (IQueryable<TInterface>)DbContext.Query<TEntity>();
        }
    }
}

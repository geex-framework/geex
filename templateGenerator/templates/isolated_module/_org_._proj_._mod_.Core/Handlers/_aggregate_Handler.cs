using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using _org_._proj_._mod_.Api.Aggregates._aggregate_s;
using _org_._proj_._mod_.Api.Aggregates._aggregate_s.Inputs;
using _org_._proj_._mod_.Core.Aggregates;
using _org_._proj_._mod_.Core.Aggregates._aggregate_s;

using Geex.Common.Abstraction.Gql.Inputs;

using MediatR;

using MongoDB.Entities;

namespace _org_._proj_._mod_.Core.Handlers
{
    public class _aggregate_Handler :
        IRequestHandler<QueryInput<I_aggregate_>, IQueryable<I_aggregate_>>,
        IRequestHandler<Create_aggregate_Request, I_aggregate_>,
        IRequestHandler<Edit_aggregate_Request, Unit>,
        IRequestHandler<Delete_aggregate_Request, Unit>
    {
        public DbContext DbContext { get; }

        public _aggregate_Handler(DbContext dbContext)
        {
            DbContext = dbContext;
        }
        public async Task<IQueryable<I_aggregate_>> Handle(QueryInput<I_aggregate_> input,
            CancellationToken cancellationToken)
        {
            return DbContext.Queryable<_aggregate_>();
        }

        public async Task<I_aggregate_> Handle(Create_aggregate_Request request, CancellationToken cancellationToken)
        {
            var entity = new _aggregate_(request.Name);
            DbContext.Attach(entity);
            await entity.SaveAsync(cancellation: cancellationToken);
            return entity;
        }

        public async Task<Unit> Handle(Edit_aggregate_Request request, CancellationToken cancellationToken)
        {
            var entity = await DbContext.Find<_aggregate_>().MatchId(request.Id).ExecuteSingleAsync(cancellationToken);
            if (!request.Name.IsNullOrEmpty())
            {
                entity.Name = request.Name;
            }
            await entity.SaveAsync(cancellation: cancellationToken);
            return Unit.Value;
        }

        public async Task<Unit> Handle(Delete_aggregate_Request request, CancellationToken cancellationToken)
        {
            await DbContext.DeleteAsync<_aggregate_>(request.Ids, cancellationToken);
            return Unit.Value;
        }
    }
}

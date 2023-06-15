using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using _org_._proj_._mod_.Api.Aggregates._aggregate_s;
using _org_._proj_._mod_.Api.Aggregates._aggregate_s.Inputs;
using _org_._proj_._mod_.Api.GqlSchemas._aggregate_s.Types;

using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Gql.Roots;

using HotChocolate;
using HotChocolate.Types;

using MediatR;

using MongoDB.Entities;

namespace _org_._proj_._mod_.Api.GqlSchemas._aggregate_s
{
    public class _aggregate_Query : Query<_aggregate_Query>
    {
        protected override void Configure(IObjectTypeDescriptor<_aggregate_Query> descriptor)
        {
            descriptor.Field(x => x._aggregate_s(default))
            .UseOffsetPaging<_aggregate_GqlType>()
            .UseFiltering<I_aggregate_>(x =>
            {
                x.Field(y => y.Name);
            })
            ;
            base.Configure(descriptor);
        }

        /// <summary>
        /// 列表获取_aggregate_
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<IQueryable<I_aggregate_>> _aggregate_s(
            )
        {
            var result = await mediator.Send(new QueryInput<I_aggregate_>());
            return result;
        }

    }
}

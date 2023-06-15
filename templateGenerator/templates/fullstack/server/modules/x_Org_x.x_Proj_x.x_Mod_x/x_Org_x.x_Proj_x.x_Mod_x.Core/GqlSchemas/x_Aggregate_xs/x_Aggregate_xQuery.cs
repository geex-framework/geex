using System;
using System.Linq;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Gql.Types;

using HotChocolate.Types;

using MongoDB.Entities;

using x_Org_x.x_Proj_x.x_Mod_x.Core.Aggregates.x_Aggregate_xs;
using x_Org_x.x_Proj_x.x_Mod_x.Core.GqlSchemas.x_Aggregate_xs.Inputs;
using x_Org_x.x_Proj_x.x_Mod_x.Core.GqlSchemas.x_Aggregate_xs.Types;

namespace x_Org_x.x_Proj_x.x_Mod_x.Core.GqlSchemas.x_Aggregate_xs
{
    public class x_Aggregate_xQuery : QueryExtension<x_Aggregate_xQuery>
    {
        private readonly DbContext _dbContext;

        public x_Aggregate_xQuery(DbContext dbContext)
        {
            _dbContext = dbContext;
        }
        protected override void Configure(IObjectTypeDescriptor<x_Aggregate_xQuery> descriptor)
        {

            descriptor.Field(x => x.x_Aggregate_xs(default))
            .UseOffsetPaging<x_Aggregate_xGqlType>()
            .UseFiltering<x_Aggregate_x>()
            .UseSorting<x_Aggregate_x>()
            ;
            base.Configure(descriptor);
        }

        /// <summary>
        /// 列表获取_aggregate_
        /// </summary>
        /// <returns></returns>
        public async Task<IQueryable<x_Aggregate_x>> x_Aggregate_xs(Queryx_Aggregate_xInput input)
        {
            var result = _dbContext.Queryable<x_Aggregate_x>()
                .WhereIf(!input.Name.IsNullOrEmpty(), x => x.Name.Contains(input.Name));
            return result;
        }

        /// <summary>
        /// 列表获取_aggregate_
        /// </summary>
        /// <returns></returns>
        public async Task<x_Aggregate_x> x_Aggregate_xById(string id)
        {
            var result = _dbContext.Queryable<x_Aggregate_x>().FirstOrDefault(x => x.Id == id);
            return result;
        }

    }
}

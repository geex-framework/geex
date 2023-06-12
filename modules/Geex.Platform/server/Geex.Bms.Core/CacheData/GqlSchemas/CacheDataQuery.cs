using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Identity.Core.Aggregates.Orgs;
using Geex.Bms.Core.CacheData.Api;
using HotChocolate.Types;
using MediatR;

namespace Geex.Bms.Core.CacheData.GqlSchemas
{
    public class CacheDataQuery : QueryExtension<CacheDataQuery>
    {
        private readonly IMediator _mediator;

        public CacheDataQuery(IMediator mediator)
        {
            this._mediator = mediator;
        }

        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<CacheDataQuery> descriptor)
        {
            base.Configure(descriptor);
        }

        public async Task<List<OrgCacheItem>> OrgsCache()
        {
            var orgs = await _mediator.Send(new QueryInput<Org>());
            return orgs.OrderBy(x => x.Code).Select(x =>
                new OrgCacheItem(x.OrgType, x.Code, x.Name)).ToList();
        }
    }
}

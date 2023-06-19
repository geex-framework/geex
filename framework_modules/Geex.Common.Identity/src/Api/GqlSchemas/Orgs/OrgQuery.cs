using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Identity.Api.GqlSchemas.Orgs.Types;
using Geex.Common.Identity.Core.Aggregates.Orgs;

using HotChocolate;
using HotChocolate.Types;

using MediatR;

namespace Geex.Common.Identity.Api.GqlSchemas.Orgs
{
    public class OrgQuery : QueryExtension<OrgQuery>
    {
        private readonly IMediator _mediator;

        public OrgQuery(IMediator mediator)
        {
            this._mediator = mediator;
        }

        protected override void Configure(IObjectTypeDescriptor<OrgQuery> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
            descriptor
                .Field(x => x.Orgs())
                .UseOffsetPaging<OrgGqlType>()
                .UseFiltering<Org>(x =>
                {
                    x.BindFieldsExplicitly();
                    x.Field(y => y.Name);
                    x.Field(y => y.Code);
                    x.Field(y => y.ParentOrgCode);
                    x.Field(y => y.OrgType);
                })
            ;
            base.Configure(descriptor);
        }
        public async Task<IQueryable<Org>> Orgs()
        {
            var orgs = await _mediator.Send(new QueryInput<Org>());
            return orgs.OrderBy(x => x.Code);
        }
    }
}

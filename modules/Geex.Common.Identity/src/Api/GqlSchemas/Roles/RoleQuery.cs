using System;
using System.Linq;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Identity.Api.Aggregates.Roles;
using Geex.Common.Identity.Api.GqlSchemas.Roles.Inputs;
using Geex.Common.Identity.Api.GqlSchemas.Roles.Types;

using HotChocolate;
using HotChocolate.Types;

using MediatR;

using MongoDB.Entities;

namespace Geex.Common.Identity.Api.GqlSchemas.Roles
{
    public class RoleQuery : QueryExtension<RoleQuery>
    {
        private readonly IMediator _mediator;

        public RoleQuery(IMediator mediator)
        {
            this._mediator = mediator;
        }

        protected override void Configure(IObjectTypeDescriptor<RoleQuery> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
            descriptor.Field(x => x.Roles())
            .UseOffsetPaging<RoleGqlType>()
            .UseFiltering<Role>(x =>
            {
                x.BindFieldsExplicitly();
                x.Field(y => y.Name);
                x.Field(y => y.Id);
                x.Field(y => y.Users);
            })
            ;
            base.Configure(descriptor);
        }
        public async Task<IQueryable<Role>> Roles(
            )
        {
            return await _mediator.Send(new QueryInput<Role>());
        }
    }
}
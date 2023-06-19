using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autofac;

using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Identity.Api.GqlSchemas.Orgs.Inputs;
using Geex.Common.Identity.Api.GqlSchemas.Roles.Inputs;
using Geex.Common.Identity.Core.Aggregates.Orgs;

using HotChocolate;
using HotChocolate.Types;

using MediatR;

using MongoDB.Entities;

namespace Geex.Common.Identity.Api.GqlSchemas.Orgs
{
    public class OrgMutation : MutationExtension<OrgMutation>
    {
        private readonly IMediator _mediator;

        public OrgMutation(IMediator mediator)
        {
            this._mediator = mediator;
        }

        protected override void Configure(IObjectTypeDescriptor<OrgMutation> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
            base.Configure(descriptor);
        }

        public async Task<Org> CreateOrg(
            CreateOrgInput input)
        {
            return await _mediator.Send(input);
        }

        public async Task<bool> FixUserOrg()
        {
            return await _mediator.Send(new FixUserOrgInput());
        }
    }
}

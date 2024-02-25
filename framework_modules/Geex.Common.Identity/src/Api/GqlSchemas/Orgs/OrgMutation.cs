using System.Threading.Tasks;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Identity.Core.Aggregates.Orgs;
using Geex.Common.Requests.Identity;
using HotChocolate.Types;

using MediatR;

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
            CreateOrgRequest request)
        {
            return await _mediator.Send(request);
        }

        public async Task<bool> FixUserOrg()
        {
            return await _mediator.Send(new FixUserOrgRequest());
        }
    }
}

using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Identity.Core.Aggregates.Orgs;
using Geex.Common.Requests.Identity;

using HotChocolate.Types;

using MediatR;

namespace Geex.Common.Identity.Api.GqlSchemas.Orgs
{
    public sealed class OrgMutation : MutationExtension<OrgMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<OrgMutation> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
            base.Configure(descriptor);
        }

        private readonly IUnitOfWork _uow;

        public OrgMutation(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        public async Task<IOrg> CreateOrg(CreateOrgRequest request) => await _uow.Request(request);

        public async Task<bool> FixUserOrg() => await _uow.Request(new FixUserOrgRequest());
    }
}

using System.Linq;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Abstractions.Entities;
using Geex.Abstractions.Gql.Types;
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
        public async Task<bool> DeleteOrg(string id)
        {
            var delete = await _uow.Query<Org>().FirstOrDefault(x => x.Id == id)?.DeleteAsync();
            return delete > 0;
        }

        public async Task<bool> FixUserOrg() => await _uow.Request(new FixUserOrgRequest());
    }
}

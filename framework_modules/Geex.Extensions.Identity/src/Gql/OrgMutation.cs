using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Requests;
using Geex.Gql;
using Geex.Gql.Types;

using HotChocolate.Types;

namespace Geex.Extensions.Identity.Gql
{
    public sealed class OrgMutation : MutationExtension<OrgMutation>, IHasDeleteMutation<Org>
    {
        protected override void Configure(IObjectTypeDescriptor<OrgMutation> descriptor)
        {
            base.Configure(descriptor);
        }

        private readonly IUnitOfWork _uow;

        public OrgMutation(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        public async Task<IOrg> CreateOrg(CreateOrgRequest request) => await _uow.Request(request);

        public async Task<IOrg> UpdateOrg(UpdateOrgRequest request) => await _uow.Request(request);

        public async Task<bool> MoveOrg(MoveOrgRequest request) => await _uow.Request(request);

        public async Task<IEnumerable<IOrg>> ImportOrg(ImportOrgRequest request) => await _uow.Request(request);

        public async Task<bool> FixUserOrg() => await _uow.Request(new FixUserOrgRequest());
    }
}

using System.Linq;
using System.Threading.Tasks;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Requests;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.Identity.Gql
{
    public sealed class OrgMutation : MutationExtension<OrgMutation>
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
        public async Task<bool> DeleteOrg(string id)
        {
            var org = _uow.Query<Org>().FirstOrDefault(x => x.Id == id);
            var delete = await org?.DeleteAsync();
            return delete > 0;
        }

        public async Task<bool> FixUserOrg() => await _uow.Request(new FixUserOrgRequest());
    }
}

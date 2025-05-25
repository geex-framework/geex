using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Entities;
using Geex.Extensions.Identity.Requests;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.Identity.Gql
{
    public sealed class RoleMutation : MutationExtension<RoleMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<RoleMutation> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
            base.Configure(descriptor);
        }

        private readonly IUnitOfWork _uow;

        public RoleMutation(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        public async Task<IRole> CreateRole(CreateRoleRequest request) => await _uow.Request(request);

        public async Task<bool> SetRoleDefault(
           SetRoleDefaultRequest request)
        {
            await _uow.Request(request);
            return true;
        }
    }
}

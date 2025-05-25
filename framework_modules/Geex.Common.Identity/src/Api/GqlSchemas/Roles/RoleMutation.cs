using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Abstractions.Entities;
using Geex.Abstractions.Gql.Types;
using Geex.Common.Identity.Api.Aggregates.Roles;
using Geex.Common.Requests.Identity;

using HotChocolate.Types;

using MediatR;

namespace Geex.Common.Identity.Api.GqlSchemas.Roles
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

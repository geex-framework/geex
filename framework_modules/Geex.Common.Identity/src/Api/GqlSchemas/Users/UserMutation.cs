using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Identity.Requests;
using Geex.Common.Requests.Identity;

using HotChocolate.Types;

using MediatR;

namespace Geex.Common.Identity.Api.GqlSchemas.Users
{
    public sealed class UserMutation : MutationExtension<UserMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<UserMutation> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
            base.Configure(descriptor);
        }

        private readonly IUnitOfWork _uow;
        public UserMutation(IUnitOfWork uow) => this._uow = uow;
        public async Task<bool> AssignRoles(AssignRoleRequest request) => await _uow.Request(request);
        public async Task<bool> AssignOrgs(AssignOrgRequest request) => await _uow.Request(request);
        public async Task<IUser> EditUser(EditUserRequest request) => await _uow.Request(request);
        public async Task<IUser> CreateUser(CreateUserRequest request) => await _uow.Request(request);
        public async Task<bool> DeleteUser(DeleteUserRequest request) => await _uow.Request(request);
        public async Task<IUser> ResetUserPassword(ResetUserPasswordRequest request) => await _uow.Request(request);
    }
}
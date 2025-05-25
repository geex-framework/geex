using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Abstractions.Entities;
using Geex.Abstractions.Gql.Types;
using Geex.Common.Identity.Requests;
using Geex.Common.Requests.Accounting;
using HotChocolate.Types;

namespace Geex.Common.Identity.Gql
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

        /// <summary>
        /// 更新设置
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> ChangePassword(ChangePasswordRequest request)
        {
            await _uow.Request(request);
            return true;
        }

        public async Task<bool> Register(RegisterUserRequest request)
        {
            await _uow.Request(request);
            return true;
        }
    }
}

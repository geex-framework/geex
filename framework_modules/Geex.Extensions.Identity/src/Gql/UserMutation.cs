using System.Threading.Tasks;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Requests;
using Geex.Extensions.Requests.Accounting;
using Geex.Gql;
using Geex.Gql.Types;

using HotChocolate.Types;

namespace Geex.Extensions.Identity.Gql
{
    public sealed class UserMutation : MutationExtension<UserMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<UserMutation> descriptor)
        {
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

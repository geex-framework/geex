using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Abstractions.Gql.Types;
using Geex.Extensions.Requests.Accounting;

using MediatR;

namespace Geex.Extensions.Accounting.GqlSchemas
{
    public sealed class AccountMutation : MutationExtension<AccountMutation>
    {
        private readonly IUnitOfWork _uow;

        public AccountMutation(IUnitOfWork uow)
        {
            this._uow = uow;
        }

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

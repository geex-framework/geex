using System.Threading.Tasks;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Requests.Accounting;
using MediatR;

namespace Geex.Common.Accounting.GqlSchemas
{
    public class AccountMutation : MutationExtension<AccountMutation>
    {
        private readonly IMediator _mediator;

        public AccountMutation(IMediator mediator)
        {
            this._mediator = mediator;
        }

        /// <summary>
        /// 更新设置
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> ChangePassword(ChangePasswordRequest request)
        {
            await _mediator.Send(request);
            return true;
        }

        public async Task<bool> Register(RegisterUserRequest request)
        {
            await _mediator.Send(request);
            return true;
        }
    }
}

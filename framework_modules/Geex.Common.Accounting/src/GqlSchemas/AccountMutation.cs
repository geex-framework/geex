﻿using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Requests.Accounting;

using MediatR;

namespace Geex.Common.Accounting.GqlSchemas
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

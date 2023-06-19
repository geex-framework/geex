using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Abstractions;
using Geex.Common.Authentication.Domain;
using Geex.Common.Authentication.GqlSchemas.Inputs;
using Geex.Common.Authentication.Utils;

using HotChocolate;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MongoDB.Entities;

using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Common.Authentication
{
    public class AuthenticationMutation : MutationExtension<AuthenticationMutation>
    {
        private readonly IMediator _mediator;
        private readonly IEnumerable<IExternalLoginProvider> _externalLoginProviders;
        private readonly GeexJwtSecurityTokenHandler _tokenHandler;
        private readonly UserTokenGenerateOptions _userTokenGenerateOptions;

        public AuthenticationMutation(IMediator mediator,
            IServiceProvider sp,
            GeexJwtSecurityTokenHandler tokenHandler,
            UserTokenGenerateOptions userTokenGenerateOptions)
        {
            this._mediator = mediator;
            this._externalLoginProviders = sp.GetServices<IExternalLoginProvider>();
            this._tokenHandler = tokenHandler;
            this._userTokenGenerateOptions = userTokenGenerateOptions;
        }

        public async Task<UserToken> Authenticate(AuthenticateInput input)
        {
            var users = await _mediator.Send(new QueryInput<IUser>());
            var user = users.MatchUserIdentifier(input.UserIdentifier?.Trim());
            if (user == default || !user.CheckPassword(input.Password))
            {
                throw new BusinessException(GeexExceptionType.NotFound, message: "用户名或者密码不正确");
            }
            if (!user.IsEnable)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "用户未激活无法登陆, 如有疑问, 请联系管理员.");
            }
            return UserToken.New(user, LoginProviderEnum.Local, _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user, LoginProviderEnum.Local, _userTokenGenerateOptions)));
        }

        public async Task<UserToken> FederateAuthenticate(FederateAuthenticateInput input)
        {
            if (input.LoginProvider == LoginProviderEnum.Local)
            {
                var userQuery = await _mediator.Send(new QueryInput<IUser>());
                var sub = _tokenHandler.ReadJwtToken(input.Code).Subject;
                var user = userQuery.MatchUserIdentifier(sub);
                var token = UserToken.New(user, input.LoginProvider, _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user, LoginProviderEnum.Local, _userTokenGenerateOptions)));
                return token;
            }
            else
            {
                var externalLoginProvider = _externalLoginProviders.FirstOrDefault(x => x.Provider == input.LoginProvider);
                if (externalLoginProvider == null)
                {
                    throw new BusinessException(GeexExceptionType.NotFound, message: "不存在的登陆提供方.");
                }
                var user = await externalLoginProvider.ExternalLogin(input.Code);
                var token = UserToken.New(user, input.LoginProvider, _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user, LoginProviderEnum.Local, _userTokenGenerateOptions)));
                return token;
            }
        }

        public async Task<bool> CancelAuthentication(
            [Service] IRedisDatabase redis,
            [Service] ClaimsPrincipal claimsPrincipal
            )
        {
            var userId = claimsPrincipal?.FindUserId();
            if (!userId.IsNullOrEmpty())
            {
                await redis.RemoveNamedAsync<UserSessionCache>(userId);
                return true;
            }
            return false;
        }
    }
}

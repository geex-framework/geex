﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Requests;
using Geex.Common.Abstraction;
using Geex.Common.Abstractions;
using Geex.Common.Authentication.Domain;
using Geex.Common.Authentication.Utils;

using MediatR;
using Geex.Common.Abstraction.Authorization;
using Geex.Common.Authentication.Requests;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Common.Authentication.Handlers
{
    public class AuthenticationHandler : IRequestHandler<AuthenticateRequest, UserToken>,IRequestHandler<FederateAuthenticateRequest, UserToken>, IRequestHandler<CancelAuthenticationRequest, bool>
    {
        private IMediator _mediator;
        private GeexJwtSecurityTokenHandler _tokenHandler;
        private UserTokenGenerateOptions _userTokenGenerateOptions;
        private readonly IEnumerable<IExternalLoginProvider> _externalLoginProviders;
        private IRedisDatabase _redis;

        public AuthenticationHandler(IMediator mediator, GeexJwtSecurityTokenHandler tokenHandler, UserTokenGenerateOptions userTokenGenerateOptions, IEnumerable<IExternalLoginProvider> externalLoginProviders, IRedisDatabase redis)
        {
            _mediator = mediator;
            _tokenHandler = tokenHandler;
            _userTokenGenerateOptions = userTokenGenerateOptions;
            _externalLoginProviders = externalLoginProviders;
            _redis = redis;
        }

        /// <inheritdoc />
        public async Task<UserToken> Handle(AuthenticateRequest request, CancellationToken cancellationToken)
        {
            var users = await _mediator.Send(new QueryRequest<IUser>(), cancellationToken);
            var user = users.MatchUserIdentifier(request.UserIdentifier?.Trim());
            if (user == default || !user.CheckPassword(request.Password))
            {
                throw new BusinessException(GeexExceptionType.NotFound, message: "用户名或者密码不正确");
            }
            if (!user.IsEnable)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "用户未激活无法登陆, 如有疑问, 请联系管理员.");
            }
            return UserToken.New(user, LoginProviderEnum.Local, _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user, LoginProviderEnum.Local, _userTokenGenerateOptions)));
        }

        /// <inheritdoc />
        public async Task<UserToken> Handle(FederateAuthenticateRequest request, CancellationToken cancellationToken)
        {
                        if (request.LoginProvider == LoginProviderEnum.Local)
            {
                var userQuery = await _mediator.Send(new QueryRequest<IUser>(), cancellationToken);
                var sub = _tokenHandler.ReadJwtToken(request.Code).Subject;
                var user = userQuery.MatchUserIdentifier(sub);
                var token = UserToken.New(user, request.LoginProvider, _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user, LoginProviderEnum.Local, _userTokenGenerateOptions)));
                return token;
            }
            else
            {
                var externalLoginProvider = _externalLoginProviders.FirstOrDefault(x => x.Provider == request.LoginProvider);
                if (externalLoginProvider == null)
                {
                    throw new BusinessException(GeexExceptionType.NotFound, message: "不存在的登陆提供方.");
                }
                var user = await externalLoginProvider.ExternalLogin(request.Code);
                var token = UserToken.New(user, request.LoginProvider, _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user, LoginProviderEnum.Local, _userTokenGenerateOptions)));
                return token;
            }

        }

        /// <inheritdoc />
        public async Task<bool> Handle(CancelAuthenticationRequest request, CancellationToken cancellationToken)
        {
            var userId = request.UserId;
            if (!userId.IsNullOrEmpty())
            {
                await _redis.RemoveNamedAsync<UserSessionCache>(userId);
                return true;
            }
            return false;
        }
    }
}

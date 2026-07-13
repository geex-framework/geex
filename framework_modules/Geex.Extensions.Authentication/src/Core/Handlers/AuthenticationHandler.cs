using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Authentication.Core.Entities;
using Geex.Extensions.Authentication.Core.Utils;
using Geex.Extensions.Authentication.Requests;
using MediatX;
using MongoDB.Bson;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.Authentication.Core.Handlers
{
    public class AuthenticationHandler :
        IRequestHandler<AuthenticateRequest, UserSession>,
        IRequestHandler<FederateAuthenticateRequest, UserSession>,
        IRequestHandler<ResolveExternalLoginRequest, ResolveExternalLoginResult>,
        IRequestHandler<LinkExternalLoginRequest, UserSession>,
        IRequestHandler<RegisterAndLinkExternalLoginRequest, UserSession>
    {
        private static readonly TimeSpan AccountLinkTokenLifetime = TimeSpan.FromMinutes(10);

        private readonly IUnitOfWork _uow;
        private readonly GeexJwtSecurityTokenHandler _tokenHandler;
        private readonly UserTokenGenerateOptions _userTokenGenerateOptions;
        private readonly IEnumerable<IExternalLoginProvider> _externalLoginProviders;
        private readonly IExternalAccountLinker _externalAccountLinker;
        private readonly IRedisDatabase _redis;
        private readonly ICurrentUser _currentUser;

        public AuthenticationHandler(
            IUnitOfWork uow,
            GeexJwtSecurityTokenHandler tokenHandler,
            UserTokenGenerateOptions userTokenGenerateOptions,
            IEnumerable<IExternalLoginProvider> externalLoginProviders,
            IExternalAccountLinker externalAccountLinker,
            IRedisDatabase redis,
            ICurrentUser currentUser)
        {
            _uow = uow;
            _tokenHandler = tokenHandler;
            _userTokenGenerateOptions = userTokenGenerateOptions;
            _externalLoginProviders = externalLoginProviders;
            _externalAccountLinker = externalAccountLinker;
            _redis = redis;
            _currentUser = currentUser;
        }

        public async Task<UserSession> Handle(AuthenticateRequest request, CancellationToken cancellationToken)
        {
            if (request.UserIdentifier is GeexConstants.SuperAdminId or GeexConstants.SuperAdminName)
            {
                _uow.DbContext.DisableAllDataFilters();
            }
            var users = _uow.Query<IAuthUser>();
            var user = users.MatchUserIdentifier(request.UserIdentifier?.Trim());
            if (user == default || !user.CheckPassword(request.Password))
            {
                throw new BusinessException(GeexExceptionType.NotFound, message: "用户名或者密码不正确");
            }

            return await BeginSessionAsync(user, LoginProviderEnum.Local, cancellationToken);
        }

        public async Task<UserSession> Handle(FederateAuthenticateRequest request, CancellationToken cancellationToken)
        {
            if (request.LoginProvider == LoginProviderEnum.Local)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "联合登录不支持本地登录提供方, 请使用 authenticate.");
            }

            var externalLoginProvider = GetProvider(request.LoginProvider);
            var externalUser = await externalLoginProvider.ExternalLogin(request.Code);
            return await BeginSessionAsync(externalUser, request.LoginProvider, cancellationToken);
        }

        public async Task<ResolveExternalLoginResult> Handle(ResolveExternalLoginRequest request, CancellationToken cancellationToken)
        {
            if (request.LoginProvider == LoginProviderEnum.Local)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "联合登录不支持本地登录提供方, 请使用 authenticate.");
            }

            var externalLoginProvider = GetProvider(request.LoginProvider);
            var identity = await externalLoginProvider.ResolveIdentity(request.Code);
            var user = _externalAccountLinker.FindByExternalLogin(identity.Provider, identity.LoginProviderId);
            if (user != null)
            {
                var session = await BeginSessionAsync(user, identity.Provider, cancellationToken);
                return new ResolveExternalLoginResult
                {
                    IsLinked = true,
                    Session = session,
                    DisplayName = identity.DisplayName,
                };
            }

            var accountLinkToken = ObjectId.GenerateNewId().ToString();
            var payload = new AccountLinkTokenPayload
            {
                LoginProvider = identity.Provider.Value,
                LoginProviderId = identity.LoginProviderId,
                DisplayName = identity.DisplayName,
                Claims = identity.Claims.ToDictionary(x => x.Type, x => x.Value),
            };
            await _redis.SetNamedAsync(payload, keyOverride: accountLinkToken, expireIn: AccountLinkTokenLifetime, token: cancellationToken);

            return new ResolveExternalLoginResult
            {
                IsLinked = false,
                AccountLinkToken = accountLinkToken,
                DisplayName = identity.DisplayName,
            };
        }

        public async Task<UserSession> Handle(LinkExternalLoginRequest request, CancellationToken cancellationToken)
        {
            var user = _currentUser.User
                ?? throw new BusinessException(GeexExceptionType.ValidationFailed, message: "请先登录本地账号后再关联外部登录.");

            var payload = await ConsumeAccountLinkTokenAsync(request.AccountLinkToken);
            var provider = LoginProviderEnum.FromValue(payload.LoginProvider);
            var claims = payload.Claims.Select(x => new Claim(x.Key, x.Value)).ToList();
            _externalAccountLinker.Link(user, provider, payload.LoginProviderId, claims);
            await _uow.SaveChanges(cancellationToken);
            return await BeginSessionAsync(user, provider, cancellationToken);
        }

        public async Task<UserSession> Handle(RegisterAndLinkExternalLoginRequest request, CancellationToken cancellationToken)
        {
            var payload = await ConsumeAccountLinkTokenAsync(request.AccountLinkToken);
            var provider = LoginProviderEnum.FromValue(payload.LoginProvider);
            var claims = payload.Claims.Select(x => new Claim(x.Key, x.Value)).ToList();
            var nickname = request.Nickname ?? payload.DisplayName;
            var user = await _externalAccountLinker.RegisterAsync(
                request.Username,
                request.Password,
                request.PhoneNumber,
                request.Email,
                nickname,
                cancellationToken);
            _externalAccountLinker.Link(user, provider, payload.LoginProviderId, claims);
            await _uow.SaveChanges(cancellationToken);
            return await BeginSessionAsync(user, provider, cancellationToken);
        }

        private IExternalLoginProvider GetProvider(LoginProviderEnum loginProvider)
        {
            var externalLoginProvider = _externalLoginProviders.FirstOrDefault(x => x.Provider == loginProvider);
            if (externalLoginProvider == null)
            {
                throw new BusinessException(GeexExceptionType.NotFound, message: "不存在的登陆提供方.");
            }

            return externalLoginProvider;
        }

        private async Task<AccountLinkTokenPayload> ConsumeAccountLinkTokenAsync(string accountLinkToken)
        {
            if (string.IsNullOrWhiteSpace(accountLinkToken))
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "AccountLinkToken 无效或已过期.");
            }

            var payload = await _redis.GetNamedAsync<AccountLinkTokenPayload>(accountLinkToken);
            if (payload == null)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "AccountLinkToken 无效或已过期.");
            }

            await _redis.RemoveNamedAsync<AccountLinkTokenPayload>(accountLinkToken);
            return payload;
        }

        private async Task<UserSession> BeginSessionAsync(IAuthUser user, LoginProviderEnum provider, CancellationToken cancellationToken)
        {
            if (!user.IsEnable)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "用户未激活无法登陆, 如有疑问, 请联系管理员.");
            }

            var token = _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user.Id, provider, _userTokenGenerateOptions));
            return await user.BeginSessionAsync(provider, token, cancellationToken);
        }
    }
}

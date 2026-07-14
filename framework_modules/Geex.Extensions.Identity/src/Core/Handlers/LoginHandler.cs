using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Authentication;
using Geex.Extensions.Authentication.Core.Entities;
using Geex.Extensions.Authentication.Core.Utils;
using Geex.Extensions.Authentication.Requests;
using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Requests;
using MediatX;
using MongoDB.Bson;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.Identity.Core.Handlers
{
    public class LoginHandler :
        IRequestHandler<FederateAuthenticateRequest, UserSession>,
        IRequestHandler<ResolveLoginRequest, ResolveLoginResult>,
        IRequestHandler<LinkLoginRequest, UserSession>,
        IRequestHandler<RegisterAndLinkLoginRequest, UserSession>,
        IRequestHandler<EnsureLocalLoginRequest, bool>
    {
        private static readonly TimeSpan UserLoginLinkTokenLifetime = TimeSpan.FromMinutes(10);

        private readonly IUnitOfWork _uow;
        private readonly GeexJwtSecurityTokenHandler _tokenHandler;
        private readonly UserTokenGenerateOptions _userTokenGenerateOptions;
        private readonly IEnumerable<ILoginProvider> _loginProviders;
        private readonly IRedisDatabase _redis;
        private readonly ICurrentUser _currentUser;

        public LoginHandler(
            IUnitOfWork uow,
            GeexJwtSecurityTokenHandler tokenHandler,
            UserTokenGenerateOptions userTokenGenerateOptions,
            IEnumerable<ILoginProvider> loginProviders,
            IRedisDatabase redis,
            ICurrentUser currentUser)
        {
            _uow = uow;
            _tokenHandler = tokenHandler;
            _userTokenGenerateOptions = userTokenGenerateOptions;
            _loginProviders = loginProviders;
            _redis = redis;
            _currentUser = currentUser;
        }

        public async Task<UserSession> Handle(FederateAuthenticateRequest request, CancellationToken cancellationToken)
        {
            var identity = await GetProvider(request.LoginProvider).ResolveUserLoginIdentity(request.Code);
            var user = await ResolveUserAsync(identity, cancellationToken)
                ?? throw new BusinessException(
                    GeexExceptionType.NotFound,
                    message: "账号尚未关联本地用户, 请使用 resolveLogin.");

            return await BeginSessionAsync(user, identity.Provider, cancellationToken);
        }

        public async Task<ResolveLoginResult> Handle(ResolveLoginRequest request, CancellationToken cancellationToken)
        {
            var identity = await GetProvider(request.LoginProvider).ResolveUserLoginIdentity(request.Code);
            var user = await ResolveUserAsync(identity, cancellationToken);
            if (user != null)
            {
                var session = await BeginSessionAsync(user, identity.Provider, cancellationToken);
                return new ResolveLoginResult
                {
                    IsLinked = true,
                    Session = session,
                    DisplayName = identity.DisplayName,
                };
            }

            var userLoginLinkToken = ObjectId.GenerateNewId().ToString();
            var payload = new UserLoginLinkTokenPayload
            {
                LoginProvider = identity.Provider.Value,
                LoginProviderId = identity.LoginProviderId,
                DisplayName = identity.DisplayName,
                Claims = identity.Claims
                    .GroupBy(x => x.Type, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.Last().Value),
            };
            await _redis.SetNamedAsync(payload, keyOverride: userLoginLinkToken, expireIn: UserLoginLinkTokenLifetime, token: cancellationToken);

            return new ResolveLoginResult
            {
                IsLinked = false,
                UserLoginLinkToken = userLoginLinkToken,
                DisplayName = identity.DisplayName,
            };
        }

        public async Task<bool> Handle(EnsureLocalLoginRequest request, CancellationToken cancellationToken)
        {
            var user = _uow.Query<User>().FirstOrDefault(x => x.Id == request.UserId);
            if (user == null)
            {
                return false;
            }

            user.UpsertLogin(LoginProviderEnum.Local, user.Id);
            await _uow.SaveChanges(cancellationToken);
            return true;
        }

        public async Task<UserSession> Handle(LinkLoginRequest request, CancellationToken cancellationToken)
        {
            var user = _currentUser.User as IUser
                ?? throw new BusinessException(GeexExceptionType.ValidationFailed, message: "请先登录本地账号后再关联登录.");

            var payload = await ConsumeUserLoginLinkTokenAsync(request.UserLoginLinkToken);
            var provider = LoginProviderEnum.FromValue(payload.LoginProvider);
            var claims = payload.Claims.Select(x => new Claim(x.Key, x.Value)).ToList();
            user.UpsertLogin(provider, payload.LoginProviderId, claims);
            await _uow.SaveChanges(cancellationToken);
            return await BeginSessionAsync(user, provider, cancellationToken);
        }

        public async Task<UserSession> Handle(RegisterAndLinkLoginRequest request, CancellationToken cancellationToken)
        {
            var payload = await ConsumeUserLoginLinkTokenAsync(request.UserLoginLinkToken);
            var provider = LoginProviderEnum.FromValue(payload.LoginProvider);
            var claims = payload.Claims.Select(x => new Claim(x.Key, x.Value)).ToList();
            var nickname = request.Nickname ?? payload.DisplayName;
            var user = await _uow.Request(new CreateUserRequest
            {
                Username = request.Username,
                Password = request.Password,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                Nickname = nickname ?? request.Username,
                IsEnable = true,
                RoleIds = [],
                OrgCodes = [],
            }, cancellationToken);
            user.UpsertLogin(provider, payload.LoginProviderId, claims);
            await _uow.SaveChanges(cancellationToken);
            return await BeginSessionAsync(user, provider, cancellationToken);
        }

        private async Task<User?> ResolveUserAsync(UserLoginIdentity identity, CancellationToken cancellationToken)
        {
            var user = UserLogin.FindUser(_uow, identity.Provider, identity.LoginProviderId);
            if (user != null)
            {
                return user;
            }

            if (identity.Provider != LoginProviderEnum.Local)
            {
                return null;
            }

            if (identity.LoginProviderId is GeexConstants.SuperAdminId)
            {
                _uow.DbContext.DisableAllDataFilters();
            }

            user = _uow.Query<IUser>().MatchUserIdentifier(identity.LoginProviderId) as User;
            if (user == null)
            {
                return null;
            }

            user.UpsertLogin(LoginProviderEnum.Local, user.Id);
            await _uow.SaveChanges(cancellationToken);
            return user;
        }

        private ILoginProvider GetProvider(LoginProviderEnum loginProvider)
        {
            var loginProviderInstance = _loginProviders.FirstOrDefault(x => x.Provider == loginProvider);
            if (loginProviderInstance == null)
            {
                throw new BusinessException(GeexExceptionType.NotFound, message: "不存在的登陆提供方.");
            }

            return loginProviderInstance;
        }

        private async Task<UserLoginLinkTokenPayload> ConsumeUserLoginLinkTokenAsync(string userLoginLinkToken)
        {
            if (string.IsNullOrWhiteSpace(userLoginLinkToken))
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "UserLoginLinkToken 无效或已过期.");
            }

            var payload = await _redis.GetNamedAsync<UserLoginLinkTokenPayload>(userLoginLinkToken);
            if (payload == null)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "UserLoginLinkToken 无效或已过期.");
            }

            await _redis.RemoveNamedAsync<UserLoginLinkTokenPayload>(userLoginLinkToken);
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

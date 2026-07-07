using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Extensions.Authentication.Core.Utils;
using Geex.Extensions.Authentication.Requests;

using MediatX;

namespace Geex.Extensions.Authentication.Core.Handlers
{
    public class AuthenticationHandler : IRequestHandler<AuthenticateRequest, UserSession>, IRequestHandler<FederateAuthenticateRequest, UserSession>
    {
        private readonly IUnitOfWork _uow;
        private readonly GeexJwtSecurityTokenHandler _tokenHandler;
        private readonly UserTokenGenerateOptions _userTokenGenerateOptions;
        private readonly IEnumerable<IExternalLoginProvider> _externalLoginProviders;

        public AuthenticationHandler(
            IUnitOfWork uow,
            GeexJwtSecurityTokenHandler tokenHandler,
            UserTokenGenerateOptions userTokenGenerateOptions,
            IEnumerable<IExternalLoginProvider> externalLoginProviders)
        {
            _uow = uow;
            _tokenHandler = tokenHandler;
            _userTokenGenerateOptions = userTokenGenerateOptions;
            _externalLoginProviders = externalLoginProviders;
        }

        /// <inheritdoc />
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
            if (!user.IsEnable)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "用户未激活无法登陆, 如有疑问, 请联系管理员.");
            }
            var token = _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user.Id, LoginProviderEnum.Local, _userTokenGenerateOptions));
            return await _uow.GetUserSession(user.Id).BeginAsync(LoginProviderEnum.Local, token, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<UserSession> Handle(FederateAuthenticateRequest request, CancellationToken cancellationToken)
        {
            request.LoginProvider ??= LoginProviderEnum.Local;
            if (request.LoginProvider == LoginProviderEnum.Local)
            {
                var sub = _tokenHandler.ReadJwtToken(request.Code).Subject;
                IDisposable disableAllDataFilters = default;
                if (sub is GeexConstants.SuperAdminId)
                {
                    disableAllDataFilters = _uow.DbContext.DisableAllDataFilters();
                }
                var userQuery = _uow.Query<IAuthUser>();
                var user = userQuery.MatchUserIdentifier(sub);
                var token = _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user.Id, LoginProviderEnum.Local, _userTokenGenerateOptions));
                var session = await _uow.GetUserSession(user.Id).BeginAsync(request.LoginProvider, token, cancellationToken);
                disableAllDataFilters?.Dispose();
                return session;
            }

            var externalLoginProvider = _externalLoginProviders.FirstOrDefault(x => x.Provider == request.LoginProvider);
            if (externalLoginProvider == null)
            {
                throw new BusinessException(GeexExceptionType.NotFound, message: "不存在的登陆提供方.");
            }
            var externalUser = await externalLoginProvider.ExternalLogin(request.Code);
            var externalToken = _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(externalUser.Id, request.LoginProvider, _userTokenGenerateOptions));
            return await _uow.GetUserSession(externalUser.Id).BeginAsync(request.LoginProvider, externalToken, cancellationToken);
        }
    }
}

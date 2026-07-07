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
        private IUnitOfWork _uow;
        private GeexJwtSecurityTokenHandler _tokenHandler;
        private UserTokenGenerateOptions _userTokenGenerateOptions;
        private readonly IEnumerable<IExternalLoginProvider> _externalLoginProviders;

        public AuthenticationHandler(IUnitOfWork uow, GeexJwtSecurityTokenHandler tokenHandler, UserTokenGenerateOptions userTokenGenerateOptions, IEnumerable<IExternalLoginProvider> externalLoginProviders)
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
            var lastUpdatedOn = await _uow.TouchUserSessionAsync(user.Id, cancellationToken);
            return UserSession.New(user, LoginProviderEnum.Local, _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user.Id, LoginProviderEnum.Local, _userTokenGenerateOptions)), lastUpdatedOn);
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
                var lastUpdatedOn = await _uow.TouchUserSessionAsync(user.Id, cancellationToken);
                var token = UserSession.New(user, request.LoginProvider, _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user.Id, LoginProviderEnum.Local, _userTokenGenerateOptions)), lastUpdatedOn);
                disableAllDataFilters?.Dispose();
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
                var lastUpdatedOn = await _uow.TouchUserSessionAsync(user.Id, cancellationToken);
                return UserSession.New(user, request.LoginProvider, _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user.Id, request.LoginProvider, _userTokenGenerateOptions)), lastUpdatedOn);
            }

        }
    }
}

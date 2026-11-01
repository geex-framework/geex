using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Authentication.Core.Entities;
using Geex.Extensions.Authentication.Core.Utils;
using Geex.Extensions.Authentication.Requests;
using MediatX;
using Microsoft.Extensions.Logging;

namespace Geex.Extensions.Authentication.Core.Handlers
{
    public class AuthenticationHandler :
        IRequestHandler<AuthenticateRequest, UserSession>
    {
        private readonly IUnitOfWork _uow;
        private readonly GeexJwtSecurityTokenHandler _tokenHandler;
        private readonly UserTokenGenerateOptions _userTokenGenerateOptions;
        private readonly ILogger<AuthenticationHandler> _logger;

        public AuthenticationHandler(
            IUnitOfWork uow,
            GeexJwtSecurityTokenHandler tokenHandler,
            UserTokenGenerateOptions userTokenGenerateOptions,
            ILogger<AuthenticationHandler> logger)
        {
            _uow = uow;
            _tokenHandler = tokenHandler;
            _userTokenGenerateOptions = userTokenGenerateOptions;
            _logger = logger;
        }

        public async Task<UserSession> Handle(AuthenticateRequest request, CancellationToken cancellationToken)
        {
            if (request.UserIdentifier is GeexConstants.SuperAdminId or GeexConstants.SuperAdminName)
            {
                _uow.DbContext.DisableAllDataFilters();
            }
            var users = _uow.Query<IAuthUser>();
            var user = users.MatchUserIdentifier(request.UserIdentifier?.Trim());
            var passwordOk = user != default && user.CheckPassword(request.Password);
            if (user == default || !passwordOk)
            {
                _logger.LogWarning("AUTH_DIAG found={Found} passwordOk={PasswordOk}", user != default, passwordOk);
                throw new BusinessException(GeexExceptionType.NotFound, message: "用户名或者密码不正确");
            }

            if (!user.IsEnable)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "用户未激活无法登陆, 如有疑问, 请联系管理员.");
            }

            var token = _tokenHandler.CreateEncodedJwt(new GeexSecurityTokenDescriptor(user.Id, LoginProviderEnum.Local, _userTokenGenerateOptions));
            var session = await user.BeginSessionAsync(LoginProviderEnum.Local, token, cancellationToken);
            await _uow.Request(new EnsureLocalLoginRequest { UserId = user.Id }, cancellationToken);
            return session;
        }
    }
}

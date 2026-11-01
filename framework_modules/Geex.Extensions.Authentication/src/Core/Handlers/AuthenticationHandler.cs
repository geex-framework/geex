using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Authentication.Core.Entities;
using Geex.Extensions.Authentication.Core.Utils;
using Geex.Extensions.Authentication.Requests;
using Geex.MultiTenant;
using MediatX;

namespace Geex.Extensions.Authentication.Core.Handlers
{
    public class AuthenticationHandler :
        IRequestHandler<AuthenticateRequest, UserSession>
    {
        private readonly IUnitOfWork _uow;
        private readonly GeexJwtSecurityTokenHandler _tokenHandler;
        private readonly UserTokenGenerateOptions _userTokenGenerateOptions;
        private readonly ICurrentTenant _currentTenant;

        public AuthenticationHandler(
            IUnitOfWork uow,
            GeexJwtSecurityTokenHandler tokenHandler,
            UserTokenGenerateOptions userTokenGenerateOptions,
            ICurrentTenant currentTenant)
        {
            _uow = uow;
            _tokenHandler = tokenHandler;
            _userTokenGenerateOptions = userTokenGenerateOptions;
            _currentTenant = currentTenant;
        }

        public async Task<UserSession> Handle(AuthenticateRequest request, CancellationToken cancellationToken)
        {
            // 登录查找不能沿用业务列表的公司/组织过滤器：匿名请求没有这些 claim，
            // 会导致合法账号被过滤掉并与“密码错误”共用提示。这里仅对凭据查找禁用
            // 数据过滤器，并显式按当前租户收敛（超管保持跨租户查找）。
            using var filterScope = _uow.DbContext.DisableAllDataFilters();
            var users = _uow.Query<IAuthUser>();
            var isSuperAdmin = request.UserIdentifier is GeexConstants.SuperAdminId or GeexConstants.SuperAdminName;
            if (!isSuperAdmin)
            {
                var tenantCode = _currentTenant.Code;
                users = users.Where(x => x.TenantCode == tenantCode);
            }
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
            var session = await user.BeginSessionAsync(LoginProviderEnum.Local, token, cancellationToken);
            await _uow.Request(new EnsureLocalLoginRequest { UserId = user.Id }, cancellationToken);
            return session;
        }
    }
}








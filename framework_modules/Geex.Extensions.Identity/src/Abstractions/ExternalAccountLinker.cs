using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Authentication;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Requests;
using Geex.Extensions.Requests.Accounting;

namespace Geex.Extensions.Identity
{
    public class ExternalAccountLinker : IExternalAccountLinker
    {
        private readonly IUnitOfWork _uow;

        public ExternalAccountLinker(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public IAuthUser? FindByExternalLogin(LoginProviderEnum provider, string loginProviderId)
        {
            return _uow.Query<User>().FindByExternalLogin(provider, loginProviderId);
        }

        public void Link(
            IAuthUser user,
            LoginProviderEnum provider,
            string loginProviderId,
            IEnumerable<Claim>? claims = null)
        {
            if (user is not IUser identityUser)
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "当前用户不支持关联外部登录.");
            }

            identityUser.UpsertExternalLogin(provider, loginProviderId, claims, _uow);
        }

        public async Task<IAuthUser> RegisterAsync(
            string username,
            string password,
            string? phoneNumber = null,
            string? email = null,
            string? nickname = null,
            CancellationToken cancellationToken = default)
        {
            return await _uow.Request(new CreateUserRequest
            {
                Username = username,
                Password = password,
                PhoneNumber = phoneNumber,
                Email = email,
                Nickname = nickname ?? username,
                IsEnable = true,
                RoleIds = [],
                OrgCodes = [],
            }, cancellationToken);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Geex.Extensions.Authentication;
using Geex.MultiTenant;
using Geex.Storage;

namespace Geex.Extensions.Identity.Core.Entities
{
    public partial class UserLogin : Entity<UserLogin>, ITenantFilteredEntity
    {
        protected UserLogin()
        {
        }

        public UserLogin(
            string userId,
            LoginProviderEnum loginProvider,
            string loginProviderId,
            IEnumerable<Claim>? providerClaims = null,
            IUnitOfWork? uow = null)
        {
            UserId = userId;
            LoginProvider = loginProvider;
            LoginProviderId = loginProviderId;
            LoginProviderClaims = providerClaims?.Select(x => new UserClaim(x.Type, x.Value)).ToList() ?? [];
            uow?.Attach(this);
        }

        public string UserId { get; private set; } = string.Empty;
        public LoginProviderEnum LoginProvider { get; private set; }
        public string LoginProviderId { get; private set; } = string.Empty;
        public List<UserClaim> LoginProviderClaims { get; private set; } = [];
        public string? TenantCode { get; set; }

        public void UpdateClaims(IEnumerable<Claim> providerClaims)
        {
            LoginProviderClaims = providerClaims.Select(x => new UserClaim(x.Type, x.Value)).ToList();
        }

        public static UserLogin? Find(
            IUnitOfWork uow,
            LoginProviderEnum provider,
            string loginProviderId)
        {
            return uow.Query<UserLogin>()
                .FirstOrDefault(x => x.LoginProvider == provider && x.LoginProviderId == loginProviderId);
        }

        public static User? FindUser(
            IUnitOfWork uow,
            LoginProviderEnum provider,
            string loginProviderId)
        {
            var login = Find(uow, provider, loginProviderId);
            if (login == null)
            {
                return null;
            }

            return uow.Query<User>().FirstOrDefault(x => x.Id == login.UserId);
        }
    }
}

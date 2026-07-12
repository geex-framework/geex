using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Geex.Extensions.Authentication;
using Geex.Storage;

namespace Geex.Extensions.Identity.Core.Entities
{
    public partial class UserExternalLogin : Entity<UserExternalLogin>, IUserExternalLogin
    {
        protected UserExternalLogin()
        {
        }

        public UserExternalLogin(
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

        public void UpdateClaims(IEnumerable<Claim> providerClaims)
        {
            LoginProviderClaims = providerClaims.Select(x => new UserClaim(x.Type, x.Value)).ToList();
        }
    }
}

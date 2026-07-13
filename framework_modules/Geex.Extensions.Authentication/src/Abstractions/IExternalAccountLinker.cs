using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Geex.Extensions.Authentication
{
    public interface IExternalAccountLinker
    {
        IAuthUser? FindByExternalLogin(LoginProviderEnum provider, string loginProviderId);

        void Link(
            IAuthUser user,
            LoginProviderEnum provider,
            string loginProviderId,
            IEnumerable<Claim>? claims = null);

        Task<IAuthUser> RegisterAsync(
            string username,
            string password,
            string? phoneNumber = null,
            string? email = null,
            string? nickname = null,
            CancellationToken cancellationToken = default);
    }
}

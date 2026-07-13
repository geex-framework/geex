using System.Threading.Tasks;

namespace Geex.Extensions.Authentication
{
    public interface IExternalLoginProvider
    {
        LoginProviderEnum Provider { get; }

        Task<ExternalLoginIdentity> ResolveIdentity(string code);

        Task<IAuthUser> ExternalLogin(string code);
    }
}

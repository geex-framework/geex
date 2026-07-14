using System.Threading.Tasks;

namespace Geex.Extensions.Authentication
{
    public interface ILoginProvider
    {
        LoginProviderEnum Provider { get; }

        Task<UserLoginIdentity> ResolveUserLoginIdentity(string code);
    }
}

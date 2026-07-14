using System.Threading.Tasks;

namespace Geex.Extensions.Authentication
{
    public abstract class LoginProviderBase : ILoginProvider
    {
        public abstract LoginProviderEnum Provider { get; }

        public abstract Task<UserLoginIdentity> ResolveUserLoginIdentity(string code);
    }
}

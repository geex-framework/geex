using System.Threading.Tasks;
using Geex.Extensions.Authentication.Core.Utils;

namespace Geex.Extensions.Authentication.Core.Providers
{
    public class LocalLoginProvider : LoginProviderBase
    {
        private readonly GeexJwtSecurityTokenHandler _tokenHandler;

        public LocalLoginProvider(GeexJwtSecurityTokenHandler tokenHandler)
        {
            _tokenHandler = tokenHandler;
        }

        public override LoginProviderEnum Provider => LoginProviderEnum.Local;

        public override Task<UserLoginIdentity> ResolveUserLoginIdentity(string code)
        {
            var sub = _tokenHandler.ReadJwtToken(code).Subject;
            return Task.FromResult(new UserLoginIdentity
            {
                Provider = LoginProviderEnum.Local,
                LoginProviderId = sub,
                DisplayName = null,
                Claims = [],
            });
        }
    }
}

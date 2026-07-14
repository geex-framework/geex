using System.Collections.Generic;
using System.Security.Claims;

namespace Geex.Extensions.Authentication
{
    public class UserLoginIdentity
    {
        public LoginProviderEnum Provider { get; set; }
        public string LoginProviderId { get; set; }
        public string? DisplayName { get; set; }
        public IReadOnlyList<Claim> Claims { get; set; } = [];
    }
}

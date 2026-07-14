using Geex.Extensions.Authentication.Core.Entities;

namespace Geex.Extensions.Authentication
{
    public class ResolveLoginResult
    {
        public bool IsLinked { get; set; }
        public UserSession? Session { get; set; }
        public string? UserLoginLinkToken { get; set; }
        public string? DisplayName { get; set; }
    }
}

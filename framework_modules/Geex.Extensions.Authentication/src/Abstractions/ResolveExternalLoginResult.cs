using Geex.Extensions.Authentication.Core.Entities;

namespace Geex.Extensions.Authentication
{
    public class ResolveExternalLoginResult
    {
        public bool IsLinked { get; set; }
        public UserSession? Session { get; set; }
        public string? AccountLinkToken { get; set; }
        public string? DisplayName { get; set; }
    }
}

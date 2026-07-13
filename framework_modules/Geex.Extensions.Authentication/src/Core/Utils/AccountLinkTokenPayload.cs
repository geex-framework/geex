using System.Collections.Generic;

namespace Geex.Extensions.Authentication.Core.Utils
{
    public class AccountLinkTokenPayload
    {
        public string LoginProvider { get; set; }
        public string LoginProviderId { get; set; }
        public string? DisplayName { get; set; }
        public Dictionary<string, string> Claims { get; set; } = new();
    }
}

using System.Collections.Generic;

namespace Geex.Extensions.Authentication
{
    public class CachedClaimEntry
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class UserSessionCache : IHasId
    {
        public string userId { get; set; } = string.Empty;
        public long Version { get; set; }
        public List<CachedClaimEntry> SupplementaryClaims { get; set; } = new();

        string IHasId.Id => userId;
    }
}

using System;
using System.Collections.Generic;

namespace Geex.Extensions.Authentication.Core.Utils;

internal class UserSessionState : IHasId
{
    public string UserId { get; set; } = string.Empty;
    internal long Version { get; set; }
    public DateTimeOffset LastUpdatedOn { get; set; }
    public List<CachedClaimEntry> SupplementaryClaims { get; set; } = new();

    string IHasId.Id => UserId;
}

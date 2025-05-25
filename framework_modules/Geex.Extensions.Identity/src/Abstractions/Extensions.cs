using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Castle.Core.Internal;

using Volo.Abp;

namespace Geex.Extensions.Identity.Abstractions
{
    public static class Extensions
    {
        public static string[]? FindOrgCodes(this ClaimsPrincipal principal)
        {
            Check.NotNull(principal, nameof(principal));
            IEnumerable<Claim> claims = principal.Claims.Where((Func<Claim, bool>)(c => c.Type == GeexClaimType.Org));
            if (claims.IsNullOrEmpty())
                return Array.Empty<string>();
            return claims.Select(x => x.Value).ToArray();
        }
    }
}

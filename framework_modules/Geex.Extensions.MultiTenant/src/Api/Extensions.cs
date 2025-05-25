using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

using Volo.Abp;

namespace Geex.Extensions.MultiTenant.Api
{
    public static class Extensions
    {
        public static string? FindTenantCode(this IIdentity identity)
        {
            Check.NotNull(identity, nameof(identity));
            Claim? claim;
            if (!(identity is ClaimsIdentity claimsIdentity))
            {
                claim = null;
            }
            else
            {
                IEnumerable<Claim> claims = claimsIdentity.Claims;
                claim = claims != null
                    ? claims.FirstOrDefault((Func<Claim, bool>)(c => c.Type == GeexClaimType.Tenant))
                    : null;
            }

            return claim?.Value;
        }

        public static string? FindTenantCode(this ClaimsPrincipal principal)
        {
            return principal.Identity?.FindTenantCode();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Geex.Extensions.Authentication;
using Geex.Extensions.Authentication.Core.Utils;
using Geex.MultiTenant;

namespace Geex.Extensions.MultiTenant.Core.Utils
{
    internal class TenantSubClaimsTransformation : ISubClaimsTransformation
    {
        /// <inheritdoc />
        public async Task<ClaimsPrincipal> TransformAsync(IAuthUser user, ClaimsPrincipal claimsPrincipal)
        {
            var claimsIdentity = claimsPrincipal.Identity as ClaimsIdentity;
            //var ownedOrgCodes = DB.Queryable<User>().Select(x => new { x.Id, x.OrgCodes }).First(x => x.Id == principal.FindUserId()).OrgCodes;
            if (user is ITenantFilteredEntity tenantFiltered)
            {
                claimsIdentity.AppendClaims(new Claim(GeexClaimType.Tenant, tenantFiltered.TenantCode ?? ""));
            }
            return claimsPrincipal;
        }
    }
}

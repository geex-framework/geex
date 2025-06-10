using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Amazon.Auth.AccessControlPolicy;

using Geex.Extensions.Authentication;
using Geex.Extensions.Authentication.Core.Utils;

namespace Geex.Extensions.Identity.Utils
{
    public class IdentitySubClaimsTransformation : ISubClaimsTransformation
    {
        /// <inheritdoc />
        public async Task<ClaimsPrincipal> TransformAsync(IUser user, ClaimsPrincipal claimsPrincipal)
        {
            var claimsIdentity = claimsPrincipal.Identity as ClaimsIdentity;
            //var ownedOrgCodes = DB.Queryable<User>().Select(x => new { x.Id, x.OrgCodes }).First(x => x.Id == principal.FindUserId()).OrgCodes;
            var ownedOrgCodes = user.OrgCodes ?? [];
            foreach (var ownedOrgCode in ownedOrgCodes)
            {
                claimsIdentity.AppendClaims(new Claim(GeexClaimType.Org, ownedOrgCode, valueType: "array"));
            }

            foreach (var role in user.RoleIds ?? [])
            {
                claimsIdentity.AppendClaims(new Claim(GeexClaimType.Role, role, valueType: "array"));
            }
            return claimsPrincipal;
        }
        /// <inheritdoc />
        public async Task<ClaimsPrincipal> TransformAsync(IAuthUser user, ClaimsPrincipal claimsPrincipal)
        {
            if (user is IUser identityUser)
            {
                return await this.TransformAsync(identityUser, claimsPrincipal);
            }
            return claimsPrincipal;
        }
    }
}

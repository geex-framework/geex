using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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
            var ownedOrgCodes = user.OrgCodes ?? [];
            foreach (var ownedOrgCode in ownedOrgCodes)
            {
                claimsIdentity.AppendClaims(new Claim(GeexClaimType.Org, ownedOrgCode, valueType: "array"));
            }

            foreach (var role in user.RoleIds ?? [])
            {
                claimsIdentity.AppendClaims(new Claim(GeexClaimType.Role, role, valueType: "array"));
            }

            var provider = claimsIdentity.GetLoginProvider();
            if (provider != LoginProviderEnum.Local && provider != LoginProviderEnum.PersonalAccessToken)
            {
                var externalLogin = user.ExternalLogins
                    .FirstOrDefault(x => x.LoginProvider == provider);
                if (externalLogin?.LoginProviderClaims?.Count > 0)
                {
                    foreach (var claim in externalLogin.LoginProviderClaims)
                    {
                        claimsIdentity.AppendClaims(new Claim(claim.ClaimType, claim.ClaimValue));
                    }
                }
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

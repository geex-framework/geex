using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Geex.Extensions.Authentication.Core.Utils
{
    public class GeexClaimsTransformation : IClaimsTransformation
    {
        private readonly IEnumerable<ISubClaimsTransformation> _transformations;
        private readonly IUnitOfWork _uow;

        public GeexClaimsTransformation(
            IEnumerable<ISubClaimsTransformation> transformations,
            IUnitOfWork uow)
        {
            _transformations = transformations;
            _uow = uow;
        }

        private UserSessionService SessionService => _uow.ServiceProvider.GetRequiredService<UserSessionService>();

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var userId = principal.FindUserId();
            if (userId.IsNullOrEmpty())
            {
                return principal;
            }

            if (principal.HasClaim(x => x.Type == GeexClaimType.Provider))
            {
                return principal;
            }

            var cachedSession = await SessionService.GetCachedSessionAsync(userId);
            var currentVersion = cachedSession?.Version ?? 0;
            var principalIdentity = principal.Identity as ClaimsIdentity;
            if (cachedSession?.SupplementaryClaims?.Count > 0 && cachedSession.Version == currentVersion)
            {
                principalIdentity.AppendClaims(cachedSession.SupplementaryClaims.Select(x => new Claim(x.Type, x.Value)));
                return principal;
            }

            using var disableAllDataFilters = _uow.DbContext.DisableAllDataFilters();
            var user = _uow.Query<IAuthUser>().GetById(userId);
            if (user == null)
            {
                return principal;
            }

            var supplementaryClaims = new List<Claim>();
            foreach (var transformation in _transformations)
            {
                var claimsPrincipal = await transformation.TransformAsync(user, principal);
                supplementaryClaims.AddRange(claimsPrincipal.Claims);
            }
            supplementaryClaims.Add(new GeexClaim(GeexClaimType.Provider, user.LoginProvider));
            principalIdentity.AppendClaims(supplementaryClaims);

            await SessionService.SetClaimCacheAsync(
                userId,
                currentVersion,
                supplementaryClaims.Select(x => new SupplementaryClaim(x.Type, x.Value)).ToList());

            return principal;
        }
    }
}

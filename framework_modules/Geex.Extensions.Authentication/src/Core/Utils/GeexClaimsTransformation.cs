using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.Authentication.Core.Utils
{
    public class GeexClaimsTransformation : IClaimsTransformation
    {
        private readonly IEnumerable<ISubClaimsTransformation> _transformations;
        private readonly IUnitOfWork _uow;
        private readonly IUserSessionVersionService _sessionVersionService;
        private readonly IRedisDatabase _redis;

        public GeexClaimsTransformation(
            IEnumerable<ISubClaimsTransformation> transformations,
            IUnitOfWork uow,
            IUserSessionVersionService sessionVersionService,
            IRedisDatabase redis)
        {
            _transformations = transformations;
            _uow = uow;
            _sessionVersionService = sessionVersionService;
            _redis = redis;
        }

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

            var currentVersion = await _sessionVersionService.GetVersionAsync(userId);
            var cachedSession = await _redis.GetNamedAsync<UserSessionCache>(userId);
            var principalIdentity = principal.Identity as ClaimsIdentity;
            if (cachedSession != null && cachedSession.Version == currentVersion && cachedSession.SupplementaryClaims?.Count > 0)
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

            await _redis.SetNamedAsync(new UserSessionCache
            {
                userId = userId,
                Version = currentVersion,
                SupplementaryClaims = supplementaryClaims.Select(x => new CachedClaimEntry { Type = x.Type, Value = x.Value }).ToList(),
            }, expireIn: TimeSpan.FromMinutes(10));

            return principal;
        }
    }
}

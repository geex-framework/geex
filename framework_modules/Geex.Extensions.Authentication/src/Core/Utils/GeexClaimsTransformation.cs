using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Geex.Extensions.Authentication.Core.Entities;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

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

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var userId = principal.FindUserId();
            if (userId.IsNullOrEmpty())
            {
                return principal;
            }

            var provider = principal.Identity is ClaimsIdentity identity
                ? identity.GetLoginProvider()
                : LoginProviderEnum.Local;
            var redis = _uow.ServiceProvider.GetRequiredService<IRedisDatabase>();
            var cached = await redis.GetNamedAsync<UserSession>(UserSession.GetCacheKey(userId, provider));
            var principalIdentity = principal.Identity as ClaimsIdentity;
            if (cached?.SupplementaryClaims?.Count > 0)
            {
                principalIdentity!.AppendClaims(cached.SupplementaryClaims.Select(x => new Claim(x.Type, x.Value)));
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
            supplementaryClaims.Add(new GeexClaim(GeexClaimType.Provider, provider));
            principalIdentity!.AppendClaims(supplementaryClaims);

            var session = user.GetSession(provider);
            if (session != null)
            {
                await session.RefreshCacheAsync(
                    supplementaryClaims.Select(x => new SupplementaryClaim(x.Type, x.Value)).ToList(),
                    TimeSpan.FromMinutes(10));
            }

            return principal;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

using Castle.Core.Internal;

using Geex.Abstractions.Authentication;
using Geex.MultiTenant;

using MongoDB.Bson;
using MongoDB.Entities.Utilities;

using Volo.Abp;

namespace Geex.Extensions.Identity
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

        public static string[]? GetOrgCodes(this ICurrentUser currentUser)
        {
            Check.NotNull(currentUser, nameof(currentUser));
            IEnumerable<Claim> claims = currentUser.ClaimsIdentity.Claims.Where((Func<Claim, bool>)(c => c.Type == GeexClaimType.Org));
            if (claims.IsNullOrEmpty())
                return Array.Empty<string>();
            return claims.Select(x => x.Value).ToArray();
        }

        public static IUser? MatchUserIdentifier(this IQueryable<IUser> users, string userIdentifier)
        {
            if (userIdentifier == GeexConstants.SuperAdminId || userIdentifier == GeexConstants.SuperAdminName)
            {
                users.Provider.As<ICachedDbContextQueryProvider>().DbContext.DisableDataFilters(typeof(ITenantFilteredEntity));
            }
            if (ObjectId.TryParse(userIdentifier, out _))
            {
                return users.FirstOrDefault(x => x.Id == userIdentifier);
            }
            return users.FirstOrDefault(x => x.PhoneNumber == userIdentifier || x.Username == userIdentifier || x.Email == userIdentifier);
        }
    }
}

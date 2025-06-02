using System;
using System.Linq;
using Geex.MultiTenant;
using MongoDB.Bson;
using MongoDB.Entities.Utilities;

namespace Geex.Extensions.Authentication
{
    public static class GeexCommonAbstractionEntitiesIUserExtensions
    {
         public static IAuthUser? MatchUserIdentifier(this IQueryable<IAuthUser> users, string userIdentifier)
        {
            if (userIdentifier is GeexConstants.SuperAdminId or GeexConstants.SuperAdminName)
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

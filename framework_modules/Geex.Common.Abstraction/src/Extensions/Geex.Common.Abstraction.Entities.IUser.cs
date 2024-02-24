using System;
using System.Linq;
using Geex.Common.Abstraction.MultiTenant;
using MongoDB.Bson;
using MongoDB.Entities.Utilities;

namespace Geex.Common.Abstraction.Entities
{
    public static class GeexCommonAbstractionEntitiesIUserExtensions
    {
         public static IUser? MatchUserIdentifier(this IQueryable<IUser> users, string userIdentifier)
        {
            if (userIdentifier == IUser.SuperAdminId || userIdentifier == IUser.SuperAdminName)
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

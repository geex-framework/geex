using System;
using System.Linq;
using Geex.Abstractions.Entities;
using Geex.Abstractions.MultiTenant;
using Geex.Common.Identity.Core.Entities;
using MongoDB.Bson;
using MongoDB.Entities.Utilities;


// ReSharper disable once CheckNamespace
namespace Geex.Common.Identity
{
    public static class GeexCommonIdentityExtensions
    {
        public static IUser? MatchUserIdentifier(this IQueryable<IUser> users, string userIdentifier)
        {
            if (userIdentifier == IUser.SuperAdminId || userIdentifier == IUser.SuperAdminName)
            {
                users.Provider.As<CachedDbContextQueryProvider<User>>().DbContext.DisableDataFilters(typeof(ITenantFilteredEntity));
            }
            if (ObjectId.TryParse(userIdentifier, out _))
            {
                return users.FirstOrDefault(x => x.Id == userIdentifier);
            }
            return users.FirstOrDefault(x => x.PhoneNumber == userIdentifier || x.Username == userIdentifier || x.Email == userIdentifier);
        }

        public static IQueryable<UserBrief>? AsBrief(this IQueryable<IUser> users)
        {
            return users.Select(x => new UserBrief(x.Email, x.Id, x.OpenId,
                x.LoginProvider, x.PhoneNumber, x.Username, x.Nickname));
        }
    }
}

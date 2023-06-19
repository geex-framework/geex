using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Identity.Core.Aggregates.Users;

using MongoDB.Bson;
using MongoDB.Entities;
using MongoDB.Entities.Utilities;


// ReSharper disable once CheckNamespace
namespace Geex.Common.Identity.Api.Aggregates.Users
{
    public static class GeexCommonIdentityApiAggregatesUsers
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

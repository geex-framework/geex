using System;
using System.Linq;

using Geex.Extensions.Identity.Core.Entities;
using Geex.MultiTenant;
using MongoDB.Bson;
using MongoDB.Entities.Utilities;


namespace Geex.Extensions.Identity
{
    public static class GeexCommonIdentityExtensions
    {
        public static IQueryable<UserBrief>? AsBrief(this IQueryable<IUser> users)
        {
            return users.Select(x => new UserBrief(x.Email, x.Id, x.OpenId,
                x.LoginProvider, x.PhoneNumber, x.Username, x.Nickname));
        }
    }
}

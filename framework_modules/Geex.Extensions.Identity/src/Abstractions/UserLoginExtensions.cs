using System;
using System.Linq;
using Geex.Extensions.Authentication;
using Geex.Extensions.Identity.Core.Entities;
using MongoDB.Entities.Utilities;

namespace Geex.Extensions.Identity
{
    public static class UserLoginExtensions
    {
        public static IUser? FindByLogin(
            this IQueryable<IUser> users,
            LoginProviderEnum provider,
            string loginProviderId)
        {
            var uow = users.Provider.As<ICachedDbContextQueryProvider>().DbContext as IUnitOfWork
                ?? throw new InvalidOperationException("Query must be backed by IUnitOfWork.");
            return UserLogin.FindUser(uow, provider, loginProviderId);
        }

        public static User? FindByLogin(
            this IQueryable<User> users,
            LoginProviderEnum provider,
            string loginProviderId)
            => FindByLogin((IQueryable<IUser>)users, provider, loginProviderId) as User;
    }
}

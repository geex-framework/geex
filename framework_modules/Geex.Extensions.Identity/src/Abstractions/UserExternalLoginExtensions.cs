using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Geex.Extensions.Authentication;
using Geex.Extensions.Identity.Core.Entities;
using MongoDB.Entities;
using MongoDB.Entities.Utilities;

namespace Geex.Extensions.Identity
{
    public static class UserExternalLoginExtensions
    {
        public static IUser? FindByExternalLogin(
            this IQueryable<IUser> users,
            LoginProviderEnum provider,
            string loginProviderId)
        {
            var dbContext = users.Provider.As<ICachedDbContextQueryProvider>().DbContext;
            var externalLogin = dbContext.Query<UserExternalLogin>()
                .FirstOrDefault(x => x.LoginProvider == provider && x.LoginProviderId == loginProviderId);
            if (externalLogin == null)
            {
                return null;
            }

            return users.FirstOrDefault(x => x.Id == externalLogin.UserId);
        }

        public static User? FindByExternalLogin(
            this IQueryable<User> users,
            LoginProviderEnum provider,
            string loginProviderId)
            => users.Cast<IUser>().FindByExternalLogin(provider, loginProviderId) as User;

        public static UserExternalLogin UpsertExternalLogin(
            this User user,
            LoginProviderEnum provider,
            string loginProviderId,
            IEnumerable<Claim>? providerClaims = null,
            IUnitOfWork? uow = null)
        {
            var unitOfWork = uow ?? (user as IEntityBase)?.DbContext as IUnitOfWork
                ?? throw new InvalidOperationException("User must be attached to a UnitOfWork.");
            var externalLogin = unitOfWork.Query<UserExternalLogin>()
                .FirstOrDefault(x => x.LoginProvider == provider && x.LoginProviderId == loginProviderId);
            if (externalLogin == null)
            {
                return new UserExternalLogin(user.Id, provider, loginProviderId, providerClaims, unitOfWork);
            }

            if (externalLogin.UserId != user.Id)
            {
                throw new BusinessException(
                    GeexExceptionType.ValidationFailed,
                    message: "该外部登录已绑定其他用户.");
            }

            unitOfWork.Attach(externalLogin);
            if (providerClaims != null)
            {
                externalLogin.UpdateClaims(providerClaims);
            }

            return externalLogin;
        }
    }
}

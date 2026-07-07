using System;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Authentication.Core.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Authentication
{
    public static class Extensions
    {
        public static ICurrentUser? GetCurrentUser(this IUnitOfWork uow)
            => uow.ServiceProvider.GetService<ICurrentUser>();

        public static IUserSession GetUserSession(this IUnitOfWork uow, string userId)
            => new UserSessionContext(uow, userId);

        public static Task<DateTimeOffset> TouchUserSessionAsync(this IUnitOfWork uow, string userId, CancellationToken cancellationToken = default)
            => uow.ServiceProvider.GetRequiredService<UserSessionService>().TouchAsync(userId, cancellationToken);

        public static Task InvalidateUserSessionAsync(this IUnitOfWork uow, string userId, CancellationToken cancellationToken = default)
            => uow.ServiceProvider.GetRequiredService<UserSessionService>().InvalidateAsync(userId, cancellationToken);
    }
}

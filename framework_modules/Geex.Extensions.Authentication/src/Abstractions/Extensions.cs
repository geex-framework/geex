using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Authentication
{
    public static class Extensions
    {
        public static ICurrentUser? GetCurrentUser(this IUnitOfWork uow)
            => uow.ServiceProvider.GetService<ICurrentUser>();

        public static IUserSession GetUserSession(this IUnitOfWork uow, string userId)
            => new UserSessionContext(uow, userId);
    }
}

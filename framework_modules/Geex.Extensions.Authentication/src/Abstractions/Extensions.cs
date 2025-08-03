using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Authentication
{
    public static class Extensions
    {

        public static ICurrentUser? GetCurrentUser(this IUnitOfWork uow)
        {
            return uow.ServiceProvider.GetService<ICurrentUser>();
        }
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Authorization
{
    public static class Extensions
    {
        public static IRbacEnforcer? GetEnforcer(this IUnitOfWork uow)
        {
            return uow.ServiceProvider.GetService<IRbacEnforcer>();
        }
    }
}

using Geex.Abstractions;

using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Settings
{
    public static class Extensions
    {
        public static string GetRedisKey(this ISetting setting)
        {
            return $"Setting:{setting.Scope}{(setting.ScopedKey == default ? "" : $":{setting.ScopedKey}")}:{setting.Name}";
        }

        public static ISettingService? GetSettingService(this IUnitOfWork uow)
        {
            return uow.ServiceProvider.GetService<ISettingService>();
        }
    }
}

using Geex.Common.Abstraction.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Common.Settings.Core
{
    public static class Extension
    {
        public static string GetRedisKey(this Setting setting)
        {
            return $"{nameof(Setting)}:{setting.Scope}{(setting.ScopedKey == default ? "" : $":{setting.ScopedKey}")}:{setting.Name}";
        }

        public static ISettingService? GetSettingService(this IUnitOfWork uow)
        {
            return uow.ServiceProvider.GetService<ISettingService>();
        }
    }
}

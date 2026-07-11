using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.Settings.Core
{
    internal static class SettingRedisExtensions
    {
        public static Task<Setting?> GetFromRedisAsync(this IRedisDatabase redis, ISetting setting)
        {
            return redis.GetAsync<Setting>(setting.GetRedisKey());
        }

        public static Task<bool> SetToRedisAsync(this IRedisDatabase redis, Setting setting)
        {
            return redis.AddAsync(setting.GetRedisKey(), setting);
        }

        public static async Task<IDictionary<string, Setting>> GetAllFromRedisByPatternAsync(this IRedisDatabase redis, string searchPattern)
        {
            var keys = await redis.SearchKeysAsync(searchPattern);
            if (keys == null || !keys.Any())
            {
                return new Dictionary<string, Setting>();
            }

            return await redis.GetAllAsync<Setting>(keys.ToHashSet());
        }
    }
}

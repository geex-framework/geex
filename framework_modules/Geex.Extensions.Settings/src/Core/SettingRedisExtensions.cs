using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

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

        public static async Task<IDictionary<string, Setting>> GetAllFromRedisByPatternAsync(this IRedisDatabase redis, string searchPattern, ILogger logger)
        {
            var keys = await redis.SearchKeysAsync(searchPattern);
            if (keys == null || !keys.Any())
            {
                return new Dictionary<string, Setting>();
            }

            var keySet = keys.ToHashSet();
            try
            {
                return await redis.GetAllAsync<Setting>(keySet);
            }
            catch (Exception ex)
            {
                var failedKeys = new List<string>();
                foreach (var key in keySet)
                {
                    try
                    {
                        await redis.GetAsync<Setting>(key);
                    }
                    catch
                    {
                        failedKeys.Add(key);
                    }
                }

                logger.LogWarningWithData(
                    $"Failed to deserialize settings from redis by pattern '{searchPattern}'. Clearing all matched keys.",
                    ex,
                    failedKeys);

                await redis.RemoveAllAsync(keySet.ToArray());
                return new Dictionary<string, Setting>();
            }
        }
    }
}

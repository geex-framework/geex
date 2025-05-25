using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Abstractions;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Entities;

using StackExchange.Redis.Extensions.Core.Abstractions;
using StackExchange.Redis.Extensions.Core.Implementations;
using StackExchange.Redis.Extensions.System.Text.Json;

// ReSharper disable once CheckNamespace
namespace StackExchange.Redis.Extensions.Core
{
    public static class Extensions
    {
        /// <summary>
        /// Add StackExchange.Redis with its serialization provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="redisConfiguration">The redis configration.</param>
        /// <typeparam name="T">The typof of serializer. <see cref="ISerializer" />.</typeparam>
        public static IServiceCollection AddStackExchangeRedisExtensions(this IServiceCollection services)
        {
            var redisConfiguration = services.GetSingletonInstance<GeexCoreModuleOptions>();
            services.AddSingleton<IRedisCacheClient, RedisCacheClient>();
            services.AddSingleton<IRedisDatabase>(x => x.GetService<IRedisCacheClient>().Db0);
            services.AddSingleton<IRedisCacheConnectionPoolManager, RedisCacheConnectionPoolManager>();
            services.AddSingleton<ISerializer, SystemTextJsonSerializer>(x => new SystemTextJsonSerializer(Json.DefaultSerializeSettings));

            services.AddSingleton((provider) =>
            {
                return provider.GetRequiredService<IRedisCacheClient>().GetDbFromConfiguration();
            });

            services.AddSingleton(redisConfiguration.Redis);

            return services;
        }

        public static string GetUniqueId(this object obj)
        {
            switch (obj)
            {
                case IEntityBase entity:
                    return entity.Id;
                case IHasId hasId:
                    return hasId.Id;
                case ClaimsPrincipal claimsPrincipal:
                    return claimsPrincipal.FindUserId();
                case IValueObject valueObject:
                    return string.Join("", valueObject.EqualityComponents.Select(x => x.GetUniqueId()));
            }

            var idProp = obj.GetType().GetProperty("Id");
            if (idProp != default)
            {
                return idProp.GetValue(obj)?.ToString() ?? "";
            }

            return obj.GetHashCode().ToString();
        }

        public static async Task<T?> GetNamedAsync<T>(this IRedisDatabase service, string key, string? @namespace = default)
        {
            var prefix = $"{typeof(T).Name}";
            if (@namespace != default)
            {
                prefix = $"{@namespace}:{prefix}";
            }
            return (await service.GetAsync<T>($"{prefix}:{key}"));
        }

        public static async Task<IDictionary<string, T>> GetAllNamedByKeyAsync<T>(this IRedisDatabase service, string? @namespace = default, string searchPattern = default)
        {
            var prefix = $"{typeof(T).Name}";
            if (@namespace != default)
            {
                prefix = $"{@namespace}:{prefix}";
            }
            var keys = await service.SearchKeysAsync($"{prefix}:{searchPattern ?? "*"}");
            var result = (await service.GetAllAsync<T>(keys));
            return result;
        }

        public static async Task<bool> RemoveNamedAsync<T>(this IRedisDatabase service, string key,
            string? @namespace = default, CommandFlags command = CommandFlags.None)
        {
            var prefix = $"{typeof(T).Name}";
            if (@namespace != default)
            {
                prefix = $"{@namespace}:{prefix}";
            }
            return await service.RemoveAsync($"{prefix}:{key}", command);
        }

        public static async Task<bool> RemoveAllNamedAsync<T>(this IRedisDatabase service, string? @namespace = default)
        {
            var prefix = $"{typeof(T).Name}";
            if (@namespace != default)
            {
                prefix = $"{@namespace}:{prefix}";
            }
            return await service.RemoveAsync($"{prefix}");
        }

        public static async Task<T> GetAndRemoveAsync<T>(this IRedisDatabase service, T obj, string? @namespace = default)
        {
            var result = await service.GetNamedAsync<T>(obj.GetUniqueId(), @namespace);
            await service.RemoveNamedAsync<T>(obj.GetUniqueId(), @namespace);
            return result;
        }

        public static async Task<bool> SetNamedAsync<T>(
            this IRedisDatabase service,
          T obj,
            string? @namespace = default,
            string? keyOverride = default,
            TimeSpan? expireIn = default,
          CancellationToken token = default(CancellationToken)) where T : class
        {
            var prefix = $"{typeof(T).Name}";
            if (@namespace != default)
            {
                prefix = $"{@namespace}:{prefix}";
            }
            if (expireIn.HasValue)
            {
                return await service.AddAsync<T>($"{prefix}:{keyOverride ?? obj.GetUniqueId()}", obj, expireIn.Value);
            }
            return await service.AddAsync<T>($"{prefix}:{keyOverride ?? obj.GetUniqueId()}", obj);
        }
    }
}

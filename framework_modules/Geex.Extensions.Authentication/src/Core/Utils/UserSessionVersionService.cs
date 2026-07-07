using System.Threading.Tasks;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.Authentication.Core.Utils;

public class UserSessionVersionService : IUserSessionVersionService
{
    private readonly IRedisDatabase _redis;

    public UserSessionVersionService(IRedisDatabase redis)
    {
        _redis = redis;
    }

    public async Task<long> GetVersionAsync(string userId)
    {
        var entry = await _redis.GetNamedAsync<UserSessionVersion>(userId);
        return entry?.Version ?? 0;
    }

    public async Task<long> BumpVersionAsync(string userId)
    {
        var version = await GetVersionAsync(userId) + 1;
        await _redis.SetNamedAsync(new UserSessionVersion { UserId = userId, Version = version });
        await _redis.RemoveNamedAsync<UserSessionCache>(userId);
        return version;
    }

    public async Task InvalidateSessionAsync(string userId)
        => await BumpVersionAsync(userId);
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.Authentication.Core.Utils;

internal class UserSessionService
{
    private readonly IRedisDatabase _redis;

    public UserSessionService(IRedisDatabase redis)
    {
        _redis = redis;
    }

    public async Task<long> GetVersionAsync(string userId, CancellationToken cancellationToken = default)
    {
        var entry = await _redis.GetNamedAsync<UserSessionState>(userId);
        return entry?.Version ?? 0;
    }

    public async Task<DateTimeOffset> GetLastUpdatedOnAsync(string userId, CancellationToken cancellationToken = default)
    {
        var entry = await _redis.GetNamedAsync<UserSessionState>(userId);
        return entry?.LastUpdatedOn ?? DateTimeOffset.MinValue;
    }

    public async Task<DateTimeOffset> TouchAsync(string userId, CancellationToken cancellationToken = default)
        => await BumpAsync(userId, cancellationToken);

    public Task InvalidateAsync(string userId, CancellationToken cancellationToken = default)
        => BumpAsync(userId, cancellationToken);

    public async Task SetClaimCacheAsync(string userId, long version, List<CachedClaimEntry> claims, CancellationToken cancellationToken = default)
    {
        var entry = await _redis.GetNamedAsync<UserSessionState>(userId);
        await _redis.SetNamedAsync(new UserSessionState
        {
            UserId = userId,
            Version = version,
            LastUpdatedOn = entry?.LastUpdatedOn ?? DateTimeOffset.UtcNow,
            SupplementaryClaims = claims,
        }, expireIn: TimeSpan.FromMinutes(10));
    }

    public async Task<UserSessionState?> GetStateAsync(string userId, CancellationToken cancellationToken = default)
        => await _redis.GetNamedAsync<UserSessionState>(userId);

    private async Task<DateTimeOffset> BumpAsync(string userId, CancellationToken cancellationToken)
    {
        var entry = await _redis.GetNamedAsync<UserSessionState>(userId);
        var version = (entry?.Version ?? 0) + 1;
        var lastUpdatedOn = DateTimeOffset.UtcNow;
        await _redis.SetNamedAsync(new UserSessionState
        {
            UserId = userId,
            Version = version,
            LastUpdatedOn = lastUpdatedOn,
        });
        return lastUpdatedOn;
    }
}

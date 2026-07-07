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

    public async Task<UserSession> BeginAsync(IAuthUser user, LoginProviderEnum provider, string token, CancellationToken cancellationToken = default)
    {
        var existing = await GetCachedSessionAsync(user.Id, cancellationToken);
        var version = (existing?.Version ?? 0) + 1;
        var session = UserSession.New(user, provider, token, DateTimeOffset.UtcNow, version);
        await _redis.SetNamedAsync(session);
        return session;
    }

    public async Task<DateTimeOffset> GetLastUpdatedOnAsync(string userId, CancellationToken cancellationToken = default)
    {
        var entry = await GetCachedSessionAsync(userId, cancellationToken);
        return entry?.LastUpdatedOn ?? DateTimeOffset.MinValue;
    }

    public Task InvalidateAsync(string userId, CancellationToken cancellationToken = default)
        => BumpAsync(userId, cancellationToken);

    public async Task SetClaimCacheAsync(string userId, long version, List<SupplementaryClaim> claims, CancellationToken cancellationToken = default)
    {
        var session = await GetCachedSessionAsync(userId, cancellationToken);
        if (session == null)
        {
            session = new UserSession { UserId = userId, LastUpdatedOn = DateTimeOffset.UtcNow };
            session.SetVersion(version);
        }
        session.ReplaceSupplementaryClaims(claims);
        await _redis.SetNamedAsync(session, expireIn: TimeSpan.FromMinutes(10));
    }

    public async Task<UserSession?> GetCachedSessionAsync(string userId, CancellationToken cancellationToken = default)
        => await _redis.GetNamedAsync<UserSession>(userId);

    private async Task BumpAsync(string userId, CancellationToken cancellationToken)
    {
        var session = await GetCachedSessionAsync(userId, cancellationToken)
            ?? new UserSession { UserId = userId };
        session.Bump();
        await _redis.SetNamedAsync(session);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Geex.Extensions.Authentication;

using Geex.Storage;

using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.Authentication.Core.Entities;

public partial class UserSession : Entity<UserSession>
{
    [JsonConstructor]
    protected UserSession()
    {
    }

    public UserSession(string userId, LoginProviderEnum provider, string token, IUnitOfWork? uow = null) : this()
    {
        UserId = userId;
        LoginProvider = provider;
        Token = token;
        LastUpdatedOn = DateTimeOffset.UtcNow;
        uow?.Attach(this);
    }

    public string UserId { get; private set; } = string.Empty;
    public LoginProviderEnum LoginProvider { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTimeOffset LastUpdatedOn { get; private set; }
    public List<SupplementaryClaim> SupplementaryClaims { get; private set; } = [];

    public void Renew(string token)
    {
        Token = token;
        LastUpdatedOn = DateTimeOffset.UtcNow;
    }

    public async Task<bool> InvalidateCacheAsync(CancellationToken cancellationToken = default)
    {
        SupplementaryClaims = [];
        var redis = Uow.ServiceProvider.GetRequiredService<IRedisDatabase>();
        return await redis.RemoveNamedAsync<UserSession>(CacheKey);
    }

    public async Task RefreshCacheAsync(
        IEnumerable<SupplementaryClaim> claims,
        TimeSpan? expireIn = null,
        CancellationToken cancellationToken = default)
    {
        SupplementaryClaims = claims.ToList();
        var redis = Uow.ServiceProvider.GetRequiredService<IRedisDatabase>();
        await redis.SetNamedAsync(
            this,
            keyOverride: CacheKey,
            expireIn: expireIn,
            token: cancellationToken);
    }

    public string CacheKey => GetCacheKey(UserId, LoginProvider);

    public static string GetCacheKey(string userId, LoginProviderEnum provider)
        => $"{userId}:{provider}";
}

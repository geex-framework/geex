using Geex.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.Favorites.Core;

internal sealed class FavoriteWriteScope : IDisposable
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(12);
    private readonly IDatabase _redis;
    private readonly IUnitOfWork _work;
    private readonly RedisValue _token = Guid.NewGuid().ToString("N");
    private readonly HashSet<RedisKey> _leases = [];
    private readonly HashSet<string> _deferredRemovals = [];

    public FavoriteWriteScope(IUnitOfWork work, IRedisDatabase redis)
    {
        _work = work;
        _redis = redis.Database;
        work.DbContext.PersistingTransaction += BeforePersisting;
        work.DbContext.TransactionAborted += Release;
        work.PostSaveChanges += AfterSave;
    }

    public async Task<Favorite?> FindForUpdate(FavoriteEntityType type, string id, CancellationToken cancellationToken)
    {
        var userId = _work.RequireUserId();
        var tenantCode = _work.ServiceProvider.GetService<ICurrentTenant>()?.Code ?? "";
        var database = _work.DbContext.DefaultDb.DatabaseNamespace.DatabaseName;
        RedisKey lease = $"Geex:Favorites:{database}:{Uri.EscapeDataString(tenantCode)}:{Uri.EscapeDataString(userId)}:{Uri.EscapeDataString(type.Value)}:{Uri.EscapeDataString(id)}";
        await Enter(lease, cancellationToken);
        var records = _work.FavoritesFor(type).Where(item => item.EntityId == id);
        if (_deferredRemovals.Count > 0)
        {
            var removedIds = _deferredRemovals.ToArray();
            records = records.Where(item => !removedIds.Contains(item.Id));
        }
        return records.SingleOrDefault();
    }

    public void ExcludeDeferredRemoval(string favoriteId)
    {
        // Without transactions a stored row remains visible in MongoDB until SaveChanges.
        if (!_work.DbContext.SupportTransaction) _deferredRemovals.Add(favoriteId);
    }

    private async Task Enter(RedisKey key, CancellationToken cancellationToken)
    {
        if (_leases.Contains(key)) return;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        while (!await _redis.LockTakeAsync(key, _token, LeaseDuration))
            await Task.Delay(TimeSpan.FromMilliseconds(50), timeout.Token);
        _leases.Add(key);
        // A competing request can read the first snapshot once the outer transaction commits.
        if (_work.DbContext.SupportTransaction)
            _work.DbContext.AfterCommit(this, ReleaseAsync);
    }

    private async Task BeforePersisting(CancellationToken token)
    {
        foreach (var key in _leases)
            if (!await _redis.LockExtendAsync(key, _token, LeaseDuration))
                throw new BusinessException(GeexExceptionType.Conflict, message: "收藏写入已超时, 请重试.");
    }

    private Task AfterSave() => _work.DbContext.SupportTransaction ? Task.CompletedTask : ReleaseAsync();

    private async Task ReleaseAsync()
    {
        var keys = _leases.ToArray();
        _leases.Clear();
        _deferredRemovals.Clear();
        foreach (var key in keys) await _redis.LockReleaseAsync(key, _token);
    }

    private void Release() => ReleaseAsync().GetAwaiter().GetResult();

    public void Dispose()
    {
        _work.DbContext.PersistingTransaction -= BeforePersisting;
        _work.DbContext.TransactionAborted -= Release;
        _work.PostSaveChanges -= AfterSave;
        Release();
    }
}

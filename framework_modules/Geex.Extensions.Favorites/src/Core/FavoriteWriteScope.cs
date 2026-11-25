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
    private readonly Dictionary<(string TenantCode, string UserId, FavoriteEntityType Type, string EntityId), Favorite?> _writes = [];
    private bool _startedTransaction;

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
        RedisKey lease = $"Geex:Favorites:{_work.DbContext.DefaultDb.DatabaseNamespace.DatabaseName}:{Uri.EscapeDataString(tenantCode)}:{Uri.EscapeDataString(userId)}";
        await Enter(lease, cancellationToken);
        return _writes.TryGetValue((tenantCode, userId, type, id), out var record) ? record :
            _work.FavoritesFor(type).SingleOrDefault(item => item.EntityId == id);
    }

    public void Remember(Favorite record, bool removed = false)
        => _writes[(record.TenantCode ?? "", record.UserId, record.EntityType, record.EntityId)] = removed ? null : record;

    private async Task Enter(RedisKey key, CancellationToken cancellationToken)
    {
        if (_leases.Contains(key)) return;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        while (!await _redis.LockTakeAsync(key, _token, LeaseDuration))
            await Task.Delay(TimeSpan.FromMilliseconds(50), timeout.Token);
        _leases.Add(key);
        // The owner lease makes concurrent callers observe the first committed title and timestamp.
        // Keep it through the outer transaction, including other fields in the same GraphQL mutation.
        if (_work.DbContext.SupportTransaction)
        {
            if (!_work.DbContext.Session.IsInTransaction)
            {
                _work.DbContext.Session.StartTransaction();
                _startedTransaction = true;
            }
            _work.DbContext.AfterCommit(this, ReleaseAsync);
        }
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
        _startedTransaction = false;
        _leases.Clear();
        _writes.Clear();
        foreach (var key in keys) await _redis.LockReleaseAsync(key, _token);
    }

    private void Release() => ReleaseAsync().GetAwaiter().GetResult();

    public void Dispose()
    {
        _work.DbContext.PersistingTransaction -= BeforePersisting;
        _work.DbContext.TransactionAborted -= Release;
        _work.PostSaveChanges -= AfterSave;
        try
        {
            if (_startedTransaction && _work.DbContext.Session.IsInTransaction) _work.DbContext.Session.AbortTransaction();
        }
        finally { Release(); }
    }
}

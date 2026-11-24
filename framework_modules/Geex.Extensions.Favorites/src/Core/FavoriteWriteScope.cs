using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.Favorites.Core;

internal sealed class FavoriteWriteScope : IDisposable
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(12);
    private readonly IDatabase _redis;
    private readonly IUnitOfWork _work;
    private readonly RedisValue _token = Guid.NewGuid().ToString("N");
    private readonly Dictionary<FavoriteOwner, RedisKey> _leases = [];
    private readonly Dictionary<(FavoriteOwner, FavoriteEntityType, string), Favorite?> _writes = [];
    private bool _startedTransaction;

    public FavoriteWriteScope(IUnitOfWork work, IRedisDatabase redis)
    {
        _work = work;
        _redis = redis.Database;
        work.DbContext.PersistingTransaction += BeforePersisting;
        work.DbContext.TransactionAborted += Release;
        work.PostSaveChanges += AfterSave;
    }

    public async Task Enter(FavoriteOwner owner, CancellationToken cancellationToken)
    {
        if (_leases.ContainsKey(owner)) return;
        RedisKey key = $"Geex:Favorites:{_work.DbContext.DefaultDb.DatabaseNamespace.DatabaseName}:{Uri.EscapeDataString(owner.Tenant)}:{Uri.EscapeDataString(owner.User)}";
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        while (!await _redis.LockTakeAsync(key, _token, LeaseDuration))
            await Task.Delay(TimeSpan.FromMilliseconds(50), timeout.Token);
        _leases.Add(owner, key);
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

    public Favorite? Find(FavoriteOwner owner, FavoriteEntityType type, string id) =>
        _writes.TryGetValue((owner, type, id), out var record) ? record :
            _work.FavoritesFor(owner, type).SingleOrDefault(item => item.EntityId == id);

    public void Remember(FavoriteOwner owner, FavoriteEntityType type, string id, Favorite? record)
        => _writes[(owner, type, id)] = record;

    private async Task BeforePersisting(CancellationToken token)
    {
        foreach (var key in _leases.Values)
            if (!await _redis.LockExtendAsync(key, _token, LeaseDuration))
                throw new BusinessException(GeexExceptionType.Conflict, message: "收藏写入已超时, 请重试.");
    }

    private Task AfterSave() => _work.DbContext.SupportTransaction ? Task.CompletedTask : ReleaseAsync();

    private async Task ReleaseAsync()
    {
        var keys = _leases.Values.ToArray();
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

using MongoDB.Bson;
using MongoDB.Driver;

namespace Geex.Extensions.Backups.Core;

internal sealed class BackupLease : IAsyncDisposable
{
    private static readonly TimeSpan Duration = TimeSpan.FromMinutes(2);
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly string _key;
    private readonly string _owner;
    private readonly CancellationTokenSource _stop = new();
    private readonly CancellationTokenSource _execution;
    private readonly Task _renewal;

    private BackupLease(IMongoCollection<BsonDocument> collection, string key, string owner, CancellationToken token)
    {
        _collection = collection;
        _key = key;
        _owner = owner;
        _execution = CancellationTokenSource.CreateLinkedTokenSource(token);
        _renewal = RenewAsync();
    }

    internal CancellationToken Token => _execution.Token;

    internal static async Task<BackupLease?> AcquireAsync(IMongoDatabase database, DateTimeOffset scheduledAt, CancellationToken token, bool deduplicateSlot = true)
    {
        var collection = database.GetCollection<BsonDocument>("_geex_database_backup_leases");
        var key = database.DatabaseNamespace.DatabaseName;
        try
        {
            await collection.InsertOneAsync(new BsonDocument
            {
                { "_id", key }, { "Owner", "" }, { "ExpiresAt", DateTime.UnixEpoch }, { "LastSlot", DateTime.UnixEpoch }
            }, cancellationToken: token);
        }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey) { }

        var owner = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var filter = Builders<BsonDocument>.Filter.Eq("_id", key) &
                     Builders<BsonDocument>.Filter.Lte("ExpiresAt", now);
        var update = Builders<BsonDocument>.Update.Set("Owner", owner).Set("ExpiresAt", now + Duration);
        if (deduplicateSlot)
        {
            filter &= Builders<BsonDocument>.Filter.Lt("LastSlot", scheduledAt.UtcDateTime);
            update = update.Set("LastSlot", scheduledAt.UtcDateTime);
        }
        var acquired = await collection.FindOneAndUpdateAsync(filter, update, cancellationToken: token);
        return acquired == null ? null : new BackupLease(collection, key, owner, token);
    }

    internal async Task EnsureOwnedAsync()
    {
        Token.ThrowIfCancellationRequested();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        var now = DateTime.UtcNow;
        var filter = Builders<BsonDocument>.Filter.Eq("_id", _key) &
                     Builders<BsonDocument>.Filter.Eq("Owner", _owner) &
                     Builders<BsonDocument>.Filter.Gt("ExpiresAt", now);
        var result = await _collection.UpdateOneAsync(filter,
            Builders<BsonDocument>.Update.Set("ExpiresAt", now + Duration), cancellationToken: timeout.Token);
        if (result.MatchedCount != 1)
        {
            _execution.Cancel();
            throw new OperationCanceledException("Database backup lease was lost.", Token);
        }
    }

    private async Task RenewAsync()
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(20));
            while (await timer.WaitForNextTickAsync(_stop.Token))
                await EnsureOwnedAsync();
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (Exception)
        {
            _execution.Cancel();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        await _renewal;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _collection.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", _key) & Builders<BsonDocument>.Filter.Eq("Owner", _owner),
                Builders<BsonDocument>.Update.Set("ExpiresAt", DateTime.UnixEpoch), cancellationToken: timeout.Token);
        }
        catch (MongoException) { /* The lease expires without a release when the database is unavailable. */ }
        catch (OperationCanceledException) { }
        finally
        {
            _execution.Dispose();
            _stop.Dispose();
        }
    }
}

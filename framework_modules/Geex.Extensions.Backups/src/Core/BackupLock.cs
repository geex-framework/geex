using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Geex.Extensions.Backups.Core;

internal sealed class BackupLock : IAsyncDisposable
{
    internal const string Key = "geex:backups:executions";
    private const string CancellationChannel = "geex:backups:cancel:";
    private readonly IDatabase _database;
    private readonly string _id = Guid.NewGuid().ToString("N");
    private readonly CancellationTokenSource _execution;
    private ChannelMessageQueue? _subscription;
    private int _disposed;

    private BackupLock(IDatabase database, CancellationToken token)
    {
        _database = database;
        _execution = CancellationTokenSource.CreateLinkedTokenSource(token);
    }

    internal CancellationToken Token => _execution.Token;

    internal static async Task<BackupLock?> AcquireAsync(IServiceProvider services, CancellationToken token, bool force = false)
    {
        token.ThrowIfCancellationRequested();
        var database = services.GetRequiredService<IRedisDatabase>().Database;
        var execution = new BackupLock(database, token);
        try
        {
            execution._subscription = await database.Multiplexer.GetSubscriber()
                .SubscribeAsync(RedisChannel.Literal(CancellationChannel + execution._id));
            execution._subscription.OnMessage(_ =>
            {
                try { execution._execution.Cancel(); }
                catch (ObjectDisposedException) { }
            });
            execution.Token.ThrowIfCancellationRequested();
            if (force)
            {
                var previous = await database.StringGetSetAsync(Key, execution._id);
                if (previous.HasValue)
                {
                    try
                    {
                        await database.Multiplexer.GetSubscriber()
                            .PublishAsync(RedisChannel.Literal(CancellationChannel + previous), "cancel");
                    }
                    catch (Exception exception)
                    {
                        services.GetService<ILogger<BackupLock>>()?.LogWarning(exception,
                            "Could not request cancellation of the previous backup. The forced backup will continue.");
                    }
                }
            }
            else if (!await database.StringSetAsync(Key, execution._id, when: When.NotExists))
            {
                await execution.DisposeAsync();
                return null;
            }
            execution.Token.ThrowIfCancellationRequested();
            return execution;
        }
        catch
        {
            await execution.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        try
        {
            await _database.LockReleaseAsync(Key, _id);
        }
        finally
        {
            try { if (_subscription != null) await _subscription.UnsubscribeAsync(); }
            finally { _execution.Dispose(); }
        }
    }
}

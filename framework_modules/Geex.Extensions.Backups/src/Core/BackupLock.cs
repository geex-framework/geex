using Microsoft.Extensions.DependencyInjection;
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
    private bool _claimAttempted;
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
            execution._claimAttempted = true;
            // Forced executions remain registered alongside their predecessors until each has actually stopped.
            var acquired = (long)await database.ScriptEvaluateAsync("""
                local previous = redis.call('HKEYS', KEYS[1])
                if #previous > 0 and ARGV[1] ~= '1' then return 0 end
                redis.call('HSET', KEYS[1], ARGV[2], ARGV[3])
                for _, id in ipairs(previous) do
                    redis.call('PUBLISH', ARGV[4] .. id, 'cancel')
                end
                return 1
                """, [Key], [force ? "1" : "0", execution._id,
                $"{Environment.MachineName}/{Environment.ProcessId}", CancellationChannel]);
            if (acquired == 0)
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
            if (_claimAttempted) await _database.HashDeleteAsync(Key, _id);
        }
        finally
        {
            try { if (_subscription != null) await _subscription.UnsubscribeAsync(); }
            finally { _execution.Dispose(); }
        }
    }
}

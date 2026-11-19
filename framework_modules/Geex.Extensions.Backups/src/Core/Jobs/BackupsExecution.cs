using Microsoft.Extensions.Hosting;
using Geex.Extensions.Backups.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;

namespace Geex.Extensions.Backups.Core.Jobs;

public sealed class BackupsExecution : IHostedService
{
    private readonly CancellationTokenSource _shutdown = new();
    private readonly object _gate = new();
    private readonly List<Task> _active = [];
    private readonly IServiceProvider _services;

    public BackupsExecution(IServiceProvider services) => _services = services;

    public async Task<bool> TryStartManualAsync(string? requestedByUserId = null, bool force = false) => await TryStartManualWithIdAsync(requestedByUserId, force) != null;

    public Task<string?> TryStartManualWithIdAsync(string? requestedByUserId = null, bool force = false)
    {
        lock (_gate)
        {
            _active.RemoveAll(task => task.IsCompleted);
            if (_shutdown.IsCancellationRequested || !force && _active.Count != 0) return Task.FromResult<string?>(null);
            var accepted = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _active.Add(RunManualAsync(accepted, requestedByUserId, force));
            return accepted.Task;
        }
    }

    private async Task RunManualAsync(TaskCompletionSource<string?> accepted, string? requestedByUserId, bool force)
    {
        try
        {
            var options = _services.GetRequiredService<BackupsModuleOptions>();
            var url = options.Validate(_services.GetRequiredService<GeexCoreModuleOptions>());
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
            stop.CancelAfter(options.Timeout);
            var scheduledAt = DateTimeOffset.UtcNow;
            await using var execution = await BackupLock.AcquireAsync(_services, stop.Token, force);
            if (execution == null)
                return;
            await ExecuteAsync(_services, url.DatabaseName, scheduledAt, options, execution.Token, automatic: false,
                accepted, requestedByUserId, cleanupFailed: !force);
        }
        catch (Exception exception)
        {
            accepted.TrySetException(exception);
            _services.GetRequiredService<ILogger<BackupsExecution>>().LogError(exception, "Manual database backup failed.");
        }
        finally
        {
            accepted.TrySetResult(null);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Backup.RecoverNotificationsAsync(_services);

    internal Task RunScheduledAsync(DateTimeOffset scheduledAt, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _active.RemoveAll(task => task.IsCompleted);
            if (_shutdown.IsCancellationRequested || _active.Count != 0) return Task.CompletedTask;
            var running = RunScheduledCoreAsync(scheduledAt, cancellationToken);
            _active.Add(running);
            return running;
        }
    }

    private async Task RunScheduledCoreAsync(DateTimeOffset scheduledAt, CancellationToken cancellationToken)
    {
        var options = _services.GetRequiredService<BackupsModuleOptions>();
        var url = options.ValidateAutomatic(_services.GetRequiredService<GeexCoreModuleOptions>());
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
        stop.CancelAfter(options.Timeout);
        await using var execution = await BackupLock.AcquireAsync(_services, stop.Token);
        if (execution == null || !await TryClaimScheduledSlotAsync(DB.Database(url.DatabaseName), scheduledAt, execution.Token))
            return;
        await ExecuteAsync(_services, url.DatabaseName, scheduledAt, options, execution.Token, automatic: true);
    }

    internal static async Task<bool> TryClaimScheduledSlotAsync(IMongoDatabase database, DateTimeOffset scheduledAt, CancellationToken token)
    {
        var collection = database.GetCollection<BsonDocument>("_geex_database_backup_schedule");
        var key = database.DatabaseNamespace.DatabaseName;
        try
        {
            await collection.InsertOneAsync(new BsonDocument { { "_id", key }, { "LastSlot", DateTime.UnixEpoch } }, cancellationToken: token);
        }
        catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey) { }
        var claimed = await collection.FindOneAndUpdateAsync(
            Builders<BsonDocument>.Filter.Eq("_id", key) & Builders<BsonDocument>.Filter.Lt("LastSlot", scheduledAt.UtcDateTime),
            Builders<BsonDocument>.Update.Set("LastSlot", scheduledAt.UtcDateTime), cancellationToken: token);
        return claimed != null;
    }

    private static async Task ExecuteAsync(IServiceProvider services, string database, DateTimeOffset scheduledAt,
        BackupsModuleOptions options, CancellationToken token, bool automatic, TaskCompletionSource<string?>? accepted = null,
        string? requestedByUserId = null, bool cleanupFailed = true)
    {
        using var scope = services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backup = new Backup(work, scheduledAt, token, cleanupFailed: cleanupFailed,
            source: automatic ? BackupSource.Automatic : BackupSource.Manual, requestedByUserId: requestedByUserId);
        var recordCreated = false;
        try
        {
            var id = await backup.WaitForRecordAsync();
            recordCreated = true;
            accepted?.TrySetResult(id);
            await work.SaveChanges(token);
        }
        catch
        {
            if (recordCreated)
                await backup.CompensateAsync(token.IsCancellationRequested ? BackupStatus.Cancelled : BackupStatus.Failed,
                    "Backup final persistence failed or execution was cancelled.");
            throw;
        }
        await Backup.PublishChangedAsync(services, backup.Id);
        await Backup.SaveResultMessageAsync(services, backup.Id);
        services.GetRequiredService<ILogger<BackupsExecution>>().LogInformation("Database backup {BackupId} completed for {DatabaseName}.", backup.Id, backup.DatabaseName);
        if (automatic)
            await PruneAsync(services, database, options.RetentionCount, token);
    }

    internal static async Task PruneAsync(IServiceProvider services, string database, int retentionCount, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var scope = services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var expired = work.Query<Backup>().Where(x => x.DatabaseName == database &&
                x.Source == BackupSource.Automatic && x.Status == BackupStatus.Succeeded &&
                (x.ErrorMessage == null || x.ErrorMessage == ""))
            .OrderByDescending(x => x.ScheduledAt).Skip(retentionCount).ToList();
        foreach (var backup in expired)
        {
            token.ThrowIfCancellationRequested();
            try { await backup.ExpireAsync(token); }
            catch (Exception exception) when (!token.IsCancellationRequested && (exception is IOException or UnauthorizedAccessException))
            {
                services.GetRequiredService<ILogger<BackupsExecution>>().LogWarning(exception,
                    "Backup {BackupId} could not be cleaned up. Explicit cleanup is required.", backup.Id);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Task[] running;
        lock (_gate)
        {
            _shutdown.Cancel();
            running = _active.ToArray();
        }
        try { await Task.WhenAll(running).WaitAsync(cancellationToken); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
    }
}

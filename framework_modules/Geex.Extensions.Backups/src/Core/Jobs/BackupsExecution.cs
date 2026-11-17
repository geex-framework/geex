using Microsoft.Extensions.Hosting;
using Geex.Extensions.Backups.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Entities;

namespace Geex.Extensions.Backups.Core.Jobs;

public sealed class BackupsExecution : IHostedService
{
    private readonly CancellationTokenSource _shutdown = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Task _active = Task.CompletedTask;
    private readonly IServiceProvider _services;

    public BackupsExecution(IServiceProvider services) => _services = services;

    public async Task<bool> TryStartManualAsync(string? requestedByUserId = null) => await TryStartManualWithIdAsync(requestedByUserId) != null;

    public Task<string?> TryStartManualWithIdAsync(string? requestedByUserId = null)
    {
        if (_shutdown.IsCancellationRequested || !_gate.Wait(0))
            return Task.FromResult<string?>(null);
        var accepted = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _active = RunManualAsync(accepted, requestedByUserId);
        return accepted.Task;
    }

    public Task<string?> TryStartOwnedManualAsync(ManualBackupExecution execution, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(execution);
        if (_shutdown.IsCancellationRequested || !_gate.Wait(0)) return Task.FromResult<string?>(null);
        var accepted = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _active = RunManualAsync(accepted, execution.RequestedByUserId, execution, cancellationToken);
        return accepted.Task;
    }

    private async Task RunManualAsync(TaskCompletionSource<string?> accepted, string? requestedByUserId,
        ManualBackupExecution? execution = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = _services.GetRequiredService<BackupsModuleOptions>();
            var url = options.Validate(_services.GetRequiredService<GeexCoreModuleOptions>());
            if (execution != null) ValidateExecution(execution, url.DatabaseName);
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token, cancellationToken);
            stop.CancelAfter(options.Timeout);
            var scheduledAt = execution?.ScheduledAt ?? DateTimeOffset.UtcNow;
            await using var lease = await BackupLease.AcquireAsync(DB.Database(url.DatabaseName), scheduledAt, stop.Token, deduplicateSlot: false);
            if (lease == null)
                return;
            if (execution != null)
            {
                var existing = await DB.Collection<Backup>().Find(x => x.Id == execution.BackupId).FirstOrDefaultAsync(lease.Token);
                if (existing != null)
                {
                    execution.EnsureMatches(existing);
                    accepted.TrySetResult(existing.Id);
                    return;
                }
            }
            await ExecuteOwnedAsync(_services, url.DatabaseName, scheduledAt, options, lease, automatic: false,
                accepted, requestedByUserId, execution);
        }
        catch (Exception exception)
        {
            accepted.TrySetException(exception);
            _services.GetRequiredService<ILogger<BackupsExecution>>().LogError(exception, "Manual database backup failed.");
        }
        finally
        {
            accepted.TrySetResult(null);
            _gate.Release();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Backup.RecoverNotificationsAsync(_services);

    public Task<bool> TryRecoverOwnedManualAsync(ManualBackupExecution execution, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(execution);
        if (_shutdown.IsCancellationRequested || !_gate.Wait(0)) return Task.FromResult(false);
        var recovery = RecoverOwnedManualAsync(execution, cancellationToken);
        _active = recovery;
        return recovery;
    }

    private async Task<bool> RecoverOwnedManualAsync(ManualBackupExecution execution, CancellationToken cancellationToken)
    {
        try
        {
            var options = _services.GetRequiredService<BackupsModuleOptions>();
            var database = options.Validate(_services.GetRequiredService<GeexCoreModuleOptions>()).DatabaseName;
            ValidateExecution(execution, database);
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token, cancellationToken);
            stop.CancelAfter(options.Timeout);
            await using var lease = await BackupLease.AcquireAsync(DB.Database(database), execution.ScheduledAt, stop.Token, deduplicateSlot: false);
            if (lease == null) return false;
            using var scope = _services.CreateScope();
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var backup = await work.DbContext.Collection<Backup>().Find(x => x.Id == execution.BackupId).FirstOrDefaultAsync(lease.Token)
                ?? throw new InvalidOperationException("Backup execution record was not found.");
            execution.EnsureMatches(backup);
            await work.Attach(backup).RecoverOwnedAsync(lease.Token, lease.EnsureOwnedAsync);
            return true;
        }
        finally { _gate.Release(); }
    }

    private static void ValidateExecution(ManualBackupExecution execution, string database)
    {
        if (execution.DatabaseName != database || DB.DefaultDb.DatabaseNamespace.DatabaseName != database)
            throw new InvalidOperationException("Backup execution must target the application's active database.");
    }

    internal Task RunScheduledAsync(DateTimeOffset scheduledAt, CancellationToken cancellationToken)
    {
        if (_shutdown.IsCancellationRequested || !_gate.Wait(0))
            return Task.CompletedTask;
        return _active = RunScheduledCoreAsync(scheduledAt, cancellationToken);
    }

    private async Task RunScheduledCoreAsync(DateTimeOffset scheduledAt, CancellationToken cancellationToken)
    {
        try
        {
            var options = _services.GetRequiredService<BackupsModuleOptions>();
            var url = options.ValidateAutomatic(_services.GetRequiredService<GeexCoreModuleOptions>());
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
            stop.CancelAfter(options.Timeout);
            await using var lease = await BackupLease.AcquireAsync(DB.Database(url.DatabaseName), scheduledAt, stop.Token);
            if (lease == null)
                return;
            await ExecuteOwnedAsync(_services, url.DatabaseName, scheduledAt, options, lease, automatic: true);
        }
        finally { _gate.Release(); }
    }
    private static async Task ExecuteOwnedAsync(IServiceProvider services, string database, DateTimeOffset scheduledAt,
        BackupsModuleOptions options, BackupLease lease, bool automatic, TaskCompletionSource<string?>? accepted = null,
        string? requestedByUserId = null, ManualBackupExecution? execution = null)
    {
        using var scope = services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backup = new Backup(work, scheduledAt, lease.Token, recoverInterrupted: execution == null,
            source: automatic ? BackupSource.Automatic : BackupSource.Manual, requestedByUserId: requestedByUserId, execution: execution);
        work.PreSaveChanges += lease.EnsureOwnedAsync;
        var recordCreated = false;
        try
        {
            var id = await backup.WaitForRecordAsync();
            recordCreated = true;
            accepted?.TrySetResult(id);
            await work.SaveChanges(lease.Token);
        }
        catch
        {
            if (recordCreated)
                await backup.CompensateAsync(lease.Token.IsCancellationRequested ? BackupStatus.Cancelled : BackupStatus.Failed,
                    "Backup final persistence failed or execution was cancelled.");
            throw;
        }
        await Backup.PublishChangedAsync(services, backup.Id);
        await Backup.SaveResultMessageAsync(services, backup.Id);
        services.GetRequiredService<ILogger<BackupsExecution>>().LogInformation("Database backup {BackupId} completed for {DatabaseName}.", backup.Id, backup.DatabaseName);
        if (automatic)
            await PruneAsync(services, database, options.RetentionCount, lease.Token, lease.EnsureOwnedAsync);
    }

    internal static async Task PruneAsync(IServiceProvider services, string database, int retentionCount, CancellationToken token,
        Func<Task>? ensureOwned = null)
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
            try { await backup.ExpireAsync(token, ensureOwned); }
            catch (Exception exception) when (!token.IsCancellationRequested && (exception is IOException or UnauthorizedAccessException))
            {
                services.GetRequiredService<ILogger<BackupsExecution>>().LogWarning(exception,
                    "Backup {BackupId} could not be cleaned up. Explicit cleanup is required.", backup.Id);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _shutdown.Cancel();
        try { await _active.WaitAsync(cancellationToken); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
    }
}

using Microsoft.Extensions.Hosting;
using Geex.Extensions.Backups.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

    private async Task RunManualAsync(TaskCompletionSource<string?> accepted, string? requestedByUserId)
    {
        try
        {
            var options = _services.GetRequiredService<BackupsModuleOptions>();
            var url = options.Validate(_services.GetRequiredService<GeexCoreModuleOptions>());
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
            stop.CancelAfter(options.Timeout);
            var scheduledAt = DateTimeOffset.UtcNow;
            await using var lease = await BackupLease.AcquireAsync(DB.Database(url.DatabaseName), scheduledAt, stop.Token, deduplicateSlot: false);
            if (lease == null)
                return;
            await ExecuteOwnedAsync(_services, url.DatabaseName, scheduledAt, options, lease, automatic: false, accepted, requestedByUserId);
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
        BackupsModuleOptions options, BackupLease lease, bool automatic, TaskCompletionSource<string?>? accepted = null, string? requestedByUserId = null)
    {
        using var scope = services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backup = new Backup(work, scheduledAt, lease.Token, recoverInterrupted: true,
            source: automatic ? BackupSource.Automatic : BackupSource.Manual, requestedByUserId: requestedByUserId);
        work.PreSaveChanges += lease.EnsureOwnedAsync;
        try
        {
            var id = await backup.WaitForRecordAsync();
            accepted?.TrySetResult(id);
            await work.SaveChanges(lease.Token);
        }
        catch
        {
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

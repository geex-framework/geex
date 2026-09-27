using Geex.Extensions.BlobStorage;
using Geex.Extensions.BlobStorage.Core.Entities;
using Geex.Extensions.BlobStorage.Requests;
using Geex.Storage;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Entities;

namespace Geex.Extensions.Backups.Core.Entities;

public partial class Backup : Entity<Backup>
{
    private readonly Task<string>? _backupTask;
    private readonly IServiceProvider? _services;
    private readonly CancellationTokenSource? _abort;
    private readonly TaskCompletionSource<string> _recordCreated = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Backup() { }

    internal Backup(IUnitOfWork uow, DateTimeOffset scheduledAt, CancellationToken cancellationToken = default,
        bool recoverInterrupted = false, BackupSource source = BackupSource.Manual, string? requestedByUserId = null)
    {
        ArgumentNullException.ThrowIfNull(uow);
        var options = uow.ServiceProvider.GetRequiredService<BackupsModuleOptions>();
        var core = uow.ServiceProvider.GetRequiredService<GeexCoreModuleOptions>();
        DatabaseName = options.Validate(core).DatabaseName;
        if (DB.DefaultDb.DatabaseNamespace.DatabaseName != DatabaseName)
            throw new InvalidOperationException("Backup target must match the application's active database.");
        ScheduledAt = scheduledAt.ToUniversalTime();
        StartedAt = DateTimeOffset.UtcNow;
        Status = BackupStatus.Running;
        Source = source;
        RequestedByUserId = source == BackupSource.Manual ? requestedByUserId : null;
        ResultMessageId = string.IsNullOrWhiteSpace(RequestedByUserId) ? null : ObjectId.GenerateNewId().ToString();
        _services = uow.ServiceProvider;
        uow.Attach(this);

        var snapshot = new Backup
        {
#pragma warning disable CS0618
            Id = Id,
#pragma warning restore CS0618
            CreatedOn = CreatedOn,
            DatabaseName = DatabaseName,
            ScheduledAt = ScheduledAt,
            StartedAt = StartedAt,
            Status = Status,
            Source = Source,
            RequestedByUserId = RequestedByUserId,
            ResultMessageId = ResultMessageId
        };
        _abort = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _backupTask = Task.Run(() => CreateArchiveAsync(snapshot, core.ConnectionString, options, recoverInterrupted, _abort.Token));
        uow.PreSaveChanges += async () =>
        {
            var blobId = await _backupTask;
            cancellationToken.ThrowIfCancellationRequested();
            if (Status == BackupStatus.Running)
            {
                BlobObjectId = blobId;
                Status = BackupStatus.Succeeded;
                FinishedAt = DateTimeOffset.UtcNow;
            }
        };
    }

    public string DatabaseName { get; private set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public BackupStatus Status { get; private set; }
    public BackupSource Source { get; private set; }
    public string? RequestedByUserId { get; private set; }
    public string? ResultMessageId { get; private set; }
    public bool ResultNotificationSaved { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? BlobObjectId { get; private set; }

    internal async Task<string> WaitForRecordAsync()
    {
        await Task.WhenAny(_recordCreated.Task, _backupTask!);
        if (!_recordCreated.Task.IsCompletedSuccessfully) await _backupTask!;
        return await _recordCreated.Task;
    }

    [BsonIgnore]
    public IBlobObject? File => BlobObjectId == null
        ? null : Uow.Query<BlobObject>().FirstOrDefault(x => x.Id == BlobObjectId);

    private async Task<string> CreateArchiveAsync(Backup snapshot, string connectionString,
        BackupsModuleOptions options, bool recoverInterrupted, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Timeout);
        var token = timeout.Token;
        var root = _services!.GetService<IHostEnvironment>()?.ContentRootPath ?? AppContext.BaseDirectory;
        var directory = GetWorkingDirectory(options.WorkingDirectory, root, Id);
        BlobObject? blob = null;
        using var scope = _services.CreateScope();
        var stage = "Record creation";
        try
        {
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await work.DbContext.Collection<Backup>().InsertOneAsync(snapshot, cancellationToken: token);
            _recordCreated.TrySetResult(Id);
            await PublishChangedAsync(_services, Id);
            stage = "Interrupted execution recovery";
            if (recoverInterrupted)
                await RecoverAsync(_services, DatabaseName, token, Id);
            stage = "Archive export";
            Directory.CreateDirectory(directory);
            var archive = Path.Combine(directory, $"backup-{Id}.archive.gz");
            await MongodumpProcess.RunAsync(options.MongodumpPath, connectionString, DatabaseName, directory, archive, token);

            stage = "Archive storage";
            blob = new BlobObject(new CreateBlobObjectRequest
            {
                File = new StreamFile(Path.GetFileName(archive), () => System.IO.File.OpenRead(archive)),
                StorageType = BlobStorageType.FileSystem
            }, work, token);
            await work.DbContext.Collection<Backup>().UpdateOneAsync(x => x.Id == Id,
                Builders<Backup>.Update.Set(x => x.BlobObjectId, blob.Id), cancellationToken: token);
            await work.SaveChanges(token);
            token.ThrowIfCancellationRequested();
            return blob.Id;
        }
        catch (Exception exception)
        {
            if (blob != null)
            {
                try { await blob.WaitForStorageAsync(); }
                catch { /* Preserve the export or persistence failure. */ }
            }
            // The outer SaveChanges cannot commit a failed task. Compensation uses a fresh scope.
            await RecordFailureAndCleanAsync(timeout.IsCancellationRequested ? BackupStatus.Cancelled : BackupStatus.Failed,
                timeout.IsCancellationRequested ? $"{stage}: backup cancelled or timed out."
                    : $"{stage}: {MongodumpProcess.SummarizeError(exception.Message, connectionString)}");
            throw;
        }
        finally
        {
            // BlobStorage owns its input stream until its background write has finished.
            if (blob != null)
            {
                try { await blob.WaitForStorageAsync(); }
                catch { /* The original failure is propagated by SaveChanges. */ }
            }
            try { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            { _services.GetService<ILogger<Backup>>()?.LogWarning("Could not remove backup temporary directory {BackupId}.", Id); }
            finally { _abort?.Dispose(); }
        }
    }

    internal async Task CompensateAsync(BackupStatus status, string message)
    {
        try { _abort?.Cancel(); }
        catch (ObjectDisposedException) { }
        if (_backupTask != null)
        {
            try { await _backupTask; }
            catch { /* The original failure has already been observed by the caller. */ }
        }
        await RecordFailureAndCleanAsync(status, message);
        await PublishChangedAsync(_services ?? ServiceProvider, Id);
        await SaveResultMessageAsync(_services ?? ServiceProvider, Id);
    }

    private async Task RecordFailureAndCleanAsync(BackupStatus status, string message)
    {
        using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            using var scope = (_services ?? ServiceProvider).CreateScope();
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var collection = work.DbContext.Collection<Backup>();
            var stored = await collection.Find(x => x.Id == Id).FirstOrDefaultAsync(cleanupTimeout.Token);
            if (stored == null || stored.Status is BackupStatus.Succeeded or BackupStatus.Expired)
                return;
            await collection.UpdateOneAsync(x => x.Id == Id && x.Status == BackupStatus.Running,
                Builders<Backup>.Update.Set(x => x.Status, status).Set(x => x.ErrorMessage, message)
                    .Set(x => x.FinishedAt, DateTimeOffset.UtcNow), cancellationToken: cleanupTimeout.Token);
            await RemoveFilesAsync(work, stored, cleanupTimeout.Token);
            await collection.UpdateOneAsync(x => x.Id == Id,
                Builders<Backup>.Update.Set(x => x.BlobObjectId, null), cancellationToken: cleanupTimeout.Token);
        }
        catch (Exception)
        {
            (_services ?? ServiceProvider).GetService<ILogger<Backup>>()?.LogError(
                "Backup {BackupId} needs cleanup on the next execution.", Id);
        }
    }

    private static async Task RemoveFilesAsync(IUnitOfWork work, Backup backup, CancellationToken token)
    {
        var fileName = $"backup-{backup.Id}.archive.gz";
        var blobId = backup.BlobObjectId;
        var blobs = await work.DbContext.Collection<BlobObject>().Find(
            x => x.Id == blobId || x.FileName == fileName).ToListAsync(token);
        foreach (var blob in blobs)
            await work.Attach(blob).DeleteAsync(token);
        await work.SaveChanges(token);
    }

    internal async Task ExpireAsync(CancellationToken cancellationToken = default, Func<Task>? ensureOwned = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ensureOwned != null) await ensureOwned();
        using var scope = ServiceProvider.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var collection = work.DbContext.Collection<Backup>();
        var stored = await collection.Find(x => x.Id == Id).FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Backup not found.");
        Status = stored.Status;
        BlobObjectId = stored.BlobObjectId;
        ErrorMessage = stored.ErrorMessage;
        if (Status == BackupStatus.Expired)
            return;
        if (Status != BackupStatus.Succeeded)
            throw new InvalidOperationException("Only a successful backup can expire.");
        try
        {
            await RemoveFilesAsync(work, stored, cancellationToken);
            if (ensureOwned != null) await ensureOwned();
            var completed = await collection.UpdateOneAsync(x => x.Id == Id && x.Status == BackupStatus.Succeeded,
                Builders<Backup>.Update.Set(x => x.Status, BackupStatus.Expired).Set(x => x.BlobObjectId, null)
                    .Set(x => x.ErrorMessage, null), cancellationToken: cancellationToken);
            if (completed.MatchedCount != 1)
            {
                var current = await collection.Find(x => x.Id == Id).FirstOrDefaultAsync(cancellationToken);
                if (current?.Status != BackupStatus.Expired)
                    throw new InvalidOperationException("Backup is no longer available for expiration.");
            }
            BlobObjectId = null;
            ErrorMessage = null;
            Status = BackupStatus.Expired;
            await PublishChangedAsync(ServiceProvider, Id);
        }
        catch (Exception exception) when (exception is not OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            if (ensureOwned != null) await ensureOwned();
            var connectionString = ServiceProvider.GetRequiredService<GeexCoreModuleOptions>().ConnectionString;
            var message = $"Archive cleanup: {MongodumpProcess.SummarizeError(exception.Message, connectionString)}";
            var recorded = await collection.UpdateOneAsync(x => x.Id == Id && x.Status == BackupStatus.Succeeded,
                Builders<Backup>.Update.Set(x => x.ErrorMessage, message), cancellationToken: cancellationToken);
            if (recorded.MatchedCount == 1)
            {
                ErrorMessage = message;
                await PublishChangedAsync(ServiceProvider, Id);
            }
            throw;
        }
    }

    internal static async Task RecoverAsync(IServiceProvider services, string database, CancellationToken token, string? activeId = null)
    {
        using var scope = services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var interrupted = work.Query<Backup>().Where(x => x.DatabaseName == database && x.Id != activeId &&
            (x.Status == BackupStatus.Running || x.Status == BackupStatus.Failed || x.Status == BackupStatus.Cancelled)).ToList();
        var options = services.GetRequiredService<BackupsModuleOptions>();
        var root = services.GetService<IHostEnvironment>()?.ContentRootPath ?? AppContext.BaseDirectory;
        await RecoverNotificationsAsync(services);
        foreach (var backup in interrupted)
        {
            token.ThrowIfCancellationRequested();
            await backup.CompensateAsync(BackupStatus.Failed, "Previous backup execution was interrupted.");
            var directory = GetWorkingDirectory(options.WorkingDirectory, root, backup.Id);
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static string GetWorkingDirectory(string workingDirectory, string root, string id)
    {
        if (!ObjectId.TryParse(id, out _))
            throw new InvalidOperationException("Backup temporary directory requires a valid backup ID.");
        return Path.Combine(Path.GetFullPath(workingDirectory, root), id);
    }

    public class BackupBsonConfig : BsonConfig<Backup>
    {
        protected override void Map(BsonClassMap<Backup> map, BsonIndexConfig<Backup> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Ascending(y => y.DatabaseName).Descending(y => y.ScheduledAt),
                options => options.Unique = true);
        }
    }
}

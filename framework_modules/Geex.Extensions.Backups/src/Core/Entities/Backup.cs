using System.Text.Json.Nodes;
using Geex.Extensions.BlobStorage;
using Geex.Extensions.BlobStorage.Core.Entities;
using Geex.Extensions.BlobStorage.Requests;
using Geex.Extensions.Messaging;
using Geex.Extensions.Messaging.Core.Entities;
using Geex.Storage;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
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
        bool cleanupFailed = false, BackupSource source = BackupSource.Manual, string? requestedByUserId = null)
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
            ModifiedOn = ModifiedOn,
            DatabaseName = DatabaseName,
            ScheduledAt = ScheduledAt,
            StartedAt = StartedAt,
            Status = Status,
            Source = Source,
            RequestedByUserId = RequestedByUserId,
            ResultMessageId = ResultMessageId
        };
        _abort = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _backupTask = Task.Run(() => CreateArchiveAsync(snapshot, core.ConnectionString, options, cleanupFailed, _abort.Token));
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
        BackupsModuleOptions options, bool cleanupFailed, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Timeout);
        var token = timeout.Token;
        var root = _services!.GetService<IHostEnvironment>()?.ContentRootPath ?? AppContext.BaseDirectory;
        var directory = GetWorkingDirectory(options.WorkingDirectory, root, Id);
        BlobObject? blob = null;
        using var scope = _services.CreateScope();
        var stage = "Record creation";
        var recordCreated = false;
        try
        {
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await work.DbContext.Collection<Backup>().InsertOneAsync(snapshot, cancellationToken: token);
            recordCreated = true;
            _recordCreated.TrySetResult(Id);
            await PublishChangedAsync(_services, Id);
            stage = "Failed backup cleanup";
            if (cleanupFailed)
                await CleanupFailedAsync(_services, DatabaseName, token);
            stage = "Archive export";
            token.ThrowIfCancellationRequested();
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
            if (recordCreated)
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
            try { if (recordCreated && Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
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
        if (backup.BlobObjectId == null) return;
        var blob = await work.DbContext.Collection<BlobObject>()
            .Find(x => x.Id == backup.BlobObjectId).FirstOrDefaultAsync(token);
        if (blob == null) return;
        await work.Attach(blob).DeleteAsync(token);
        await work.SaveChanges(token);
    }

    internal async Task ExpireAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
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

    internal static async Task CleanupFailedAsync(IServiceProvider services, string database, CancellationToken token)
    {
        using var scope = services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var failed = work.Query<Backup>().Where(x => x.DatabaseName == database &&
            (x.Status == BackupStatus.Failed || x.Status == BackupStatus.Cancelled)).ToList();
        var options = services.GetRequiredService<BackupsModuleOptions>();
        var root = services.GetService<IHostEnvironment>()?.ContentRootPath ?? AppContext.BaseDirectory;
        await RecoverNotificationsAsync(services);
        foreach (var backup in failed)
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

    internal const string ChangedTopic = "BackupsChanged";

    internal static async Task PublishChangedAsync(IServiceProvider services, string id)
    {
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var sender = services.GetService<ITopicEventSender>();
            if (sender != null) await sender.SendAsync(ChangedTopic, id, deadline.Token);
        }
        catch (Exception exception)
        {
            services.GetService<ILogger<Backup>>()?.LogWarning(exception, "Backup {BackupId} saved but change notification failed.", id);
        }
    }

    internal static async Task RecoverNotificationsAsync(IServiceProvider services)
    {
        try
        {
            using var scope = services.CreateScope();
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var pending = await work.DbContext.Collection<Backup>().Find(x => x.Source == BackupSource.Manual &&
                x.ResultMessageId != null && x.Status != BackupStatus.Running)
                .ToListAsync(deadline.Token);
            foreach (var backup in pending)
            {
                await RepairResultMessageTimeAsync(work, backup, deadline.Token);
                if (!backup.ResultNotificationSaved) await SaveResultMessageAsync(services, backup.Id);
            }
        }
        catch (Exception exception)
        {
            services.GetService<ILogger<Backup>>()?.LogWarning(exception, "Pending backup messages will be retried during recovery.");
        }
    }

    private static DateTimeOffset ResultMessageTime(Backup backup) =>
        (backup.FinishedAt is { } finished && finished > DateTimeOffset.MinValue ? finished :
            backup.StartedAt > DateTimeOffset.MinValue ? backup.StartedAt : DateTimeOffset.UtcNow).ToUniversalTime();

    private static async Task RepairResultMessageTimeAsync(IUnitOfWork work, Backup backup, CancellationToken token)
    {
        var time = ResultMessageTime(backup);
        // Only repair timestamps omitted by the previous direct-insert implementation.
        await work.DbContext.Collection<Message>().UpdateOneAsync(x => x.Id == backup.ResultMessageId && x.CreatedOn == DateTimeOffset.MinValue,
            Builders<Message>.Update.Set(x => x.CreatedOn, time), cancellationToken: token);
        await work.DbContext.Collection<Message>().UpdateOneAsync(x => x.Id == backup.ResultMessageId && x.ModifiedOn == DateTimeOffset.MinValue,
            Builders<Message>.Update.Set(x => x.ModifiedOn, time), cancellationToken: token);
        await work.DbContext.Collection<MessageDistribution>().UpdateOneAsync(x => x.Id == backup.ResultMessageId && x.CreatedOn == DateTimeOffset.MinValue,
            Builders<MessageDistribution>.Update.Set(x => x.CreatedOn, time), cancellationToken: token);
        await work.DbContext.Collection<MessageDistribution>().UpdateOneAsync(x => x.Id == backup.ResultMessageId && x.ModifiedOn == DateTimeOffset.MinValue,
            Builders<MessageDistribution>.Update.Set(x => x.ModifiedOn, time), cancellationToken: token);
    }

    internal static async Task SaveResultMessageAsync(IServiceProvider services, string id)
    {
        try
        {
            using var scope = services.CreateScope();
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var token = deadline.Token;
            var collection = work.DbContext.Collection<Backup>();
            var backup = await collection.Find(x => x.Id == id).FirstOrDefaultAsync(token);
            if (backup == null || backup.Source != BackupSource.Manual || backup.Status == BackupStatus.Running ||
                backup.ResultNotificationSaved || string.IsNullOrWhiteSpace(backup.RequestedByUserId) || backup.ResultMessageId == null) return;
            var outcome = backup.Status == BackupStatus.Expired ? BackupStatus.Succeeded : backup.Status;
            var severity = outcome == BackupStatus.Succeeded ? MessageSeverityType.Success :
                outcome == BackupStatus.Cancelled ? MessageSeverityType.Warn : MessageSeverityType.Error;
            var label = outcome == BackupStatus.Succeeded ? "成功" : outcome == BackupStatus.Cancelled ? "已取消" : "失败";
            var messageTime = ResultMessageTime(backup);
            var message = new Message($"数据库 {backup.DatabaseName} 的手动备份{label}.", severity, new JsonObject
            {
                ["actions"] = new JsonArray(new JsonObject { ["key"] = "backups.view", ["backupId"] = id }),
                ["backupId"] = id, ["status"] = outcome.ToString()
            });
#pragma warning disable CS0618
            message.Id = backup.ResultMessageId;
            message.CreatedOn = message.ModifiedOn = messageTime;
            var distribution = new MessageDistribution(message.Id, backup.RequestedByUserId)
            { Id = message.Id, CreatedOn = messageTime, ModifiedOn = messageTime };
#pragma warning restore CS0618
            // Stable IDs and insert-only writes survive partial saves without resetting IsRead.
            await work.DbContext.Collection<Message>().UpdateOneAsync(x => x.Id == message.Id,
                new BsonDocument("$setOnInsert", message.ToBsonDocument()), new UpdateOptions { IsUpsert = true }, token);
            await work.DbContext.Collection<MessageDistribution>().UpdateOneAsync(x => x.Id == distribution.Id,
                new BsonDocument("$setOnInsert", distribution.ToBsonDocument()), new UpdateOptions { IsUpsert = true }, token);
            var marked = await collection.UpdateOneAsync(x => x.Id == id && !x.ResultNotificationSaved,
                Builders<Backup>.Update.Set(x => x.ResultNotificationSaved, true), cancellationToken: token);
            if (marked.ModifiedCount == 1 && services.GetService<ITopicEventSender>() != null)
                await work.ClientNotify(new NewMessageClientNotify(message), token, backup.RequestedByUserId);
        }
        catch (Exception exception)
        {
            services.GetService<ILogger<Backup>>()?.LogWarning(exception, "Backup {BackupId} result notification needs recovery or reconnect.", id);
        }
    }
}

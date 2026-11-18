using System.Text.Json;
using Geex.Extensions.Backups.Core;
using Geex.Extensions.Backups.Core.Entities;
using Geex.Extensions.Backups.Core.Jobs;
using Geex.Extensions.BlobStorage;
using Geex.Extensions.BlobStorage.Core.Entities;
using Geex.Extensions.BlobStorage.Requests;
using Geex.Extensions.Messaging.Core.Entities;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Entities;
using Xunit;

namespace Geex.Extensions.Backups.Tests;

[Collection("Backups")]
public sealed class ManualBackupExecutionTests(BackupFixture fixture) : IAsyncLifetime
{
    private static long _slot;
    private readonly List<string> _ids = [];
    private readonly List<string> _blobIds = [];
    private readonly List<BackupsExecution> _workers = [];

    private ManualBackupExecution Identity(string? key = null)
    {
        var identity = new ManualBackupExecution(key ?? "owned-test/" + Guid.NewGuid().ToString("N"), fixture.DatabaseName,
            ObjectId.GenerateNewId().ToString(), new DateTimeOffset(2033, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(Interlocked.Increment(ref _slot)));
        _ids.Add(identity.BackupId);
        return identity;
    }

    private BackupsExecution Worker()
    {
        var worker = new BackupsExecution(fixture.Services);
        _workers.Add(worker);
        return worker;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var worker in _workers) await worker.StopAsync(CancellationToken.None);
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backups = await DB.Collection<Backup>().Find(Builders<Backup>.Filter.In(x => x.Id, _ids)).ToListAsync();
        var blobIds = backups.Where(x => x.BlobObjectId != null).Select(x => x.BlobObjectId!).Concat(_blobIds).ToArray();
        var blobs = await DB.Collection<BlobObject>().Find(Builders<BlobObject>.Filter.In(x => x.Id, blobIds) |
            Builders<BlobObject>.Filter.In(x => x.StorageOwnerTag, _ids.Select(id => "geex.backup/" + id))).ToListAsync();
        foreach (var blob in blobs) await work.Attach(blob).DeleteAsync();
        await work.SaveChanges();
        var messageIds = backups.Select(x => x.ResultMessageId).ToArray();
        await DB.Collection<MessageDistribution>().DeleteManyAsync(Builders<MessageDistribution>.Filter.In(x => x.MessageId, messageIds));
        await DB.Collection<Message>().DeleteManyAsync(Builders<Message>.Filter.In(x => x.Id, messageIds));
        await DB.Collection<Backup>().DeleteManyAsync(Builders<Backup>.Filter.In(x => x.Id, _ids));
        foreach (var id in _ids)
        {
            var directory = Path.Combine(fixture.Options.WorkingDirectory, id);
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task PersistedIdentitySurvivesLostAcceptanceAndThreeConcurrentRetries()
    {
        var intended = Identity();
        var execution = JsonSerializer.Deserialize<ManualBackupExecution>(JsonSerializer.Serialize(intended))!;
        Assert.Equal(intended, execution);
        var workers = Enumerable.Range(0, 3).Select(_ => Worker()).ToArray();
        try
        {
            var accepted = await Task.WhenAll(workers.Select(x => x.TryStartManualWithIdAsync(execution, CancellationToken.None)));
            Assert.Contains(execution.BackupId, accepted);
            Assert.All(accepted.Where(x => x != null), id => Assert.Equal(execution.BackupId, id));
            var completed = await WaitForCompletion(execution.BackupId);
            Assert.True(execution.Matches(completed));
            Assert.Equal(BackupStatus.Succeeded, completed.Status);
            Assert.True(completed.CreatedOn > DateTimeOffset.MinValue);
            var snapshot = completed.ToBson();
            var retry = Worker();
            Assert.Equal(execution.BackupId, await retry.TryStartManualWithIdAsync(execution, CancellationToken.None));
            await retry.StopAsync(CancellationToken.None);
            Assert.Equal(snapshot, (await Find(execution.BackupId)).ToBson());
            Assert.Equal(1L, await DB.Collection<Backup>().CountDocumentsAsync(x => x.ExecutionKey == execution.ExecutionKey));
            Assert.Equal(1L, await DB.Collection<BlobObject>().CountDocumentsAsync(x => x.FileName == "backup-" + execution.BackupId + ".archive.gz"));
            Assert.Equal(1L, await DB.Collection<MessageDistribution>().CountDocumentsAsync(x => x.MessageId == completed.ResultMessageId));
        }
        finally { foreach (var worker in workers) await worker.StopAsync(CancellationToken.None); }
    }

    [Fact]
    public async Task InterruptedAttemptRecoveryAndNextAttemptLeaveExistingBackupsFilesAndRecipientsUntouched()
    {
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var sentinels = new List<(Backup Backup, byte[] Snapshot, string Path, string Directory)>();
        foreach (var status in new[] { BackupStatus.Running, BackupStatus.Failed, BackupStatus.Cancelled, BackupStatus.Succeeded })
        {
            var identity = Identity();
            var blob = new BlobObject(new CreateBlobObjectRequest
            {
                File = new StreamFile("backup-" + identity.BackupId + ".archive.gz", () => new MemoryStream([1, 2, 3, (byte)status])),
                StorageType = BlobStorageType.FileSystem
            }, work);
            await work.SaveChanges();
            var sentinel = Sample(identity, status, blob.Id, owned: false);
            await DB.Collection<Backup>().InsertOneAsync(sentinel);
            var directory = Path.Combine(fixture.Options.WorkingDirectory, sentinel.Id);
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, "partial"), "existing backup work");
            sentinels.Add((sentinel, (await Find(sentinel.Id)).ToBson(), blob.GetFilePath(), directory));
        }

        var first = Identity("fault-test/" + Guid.NewGuid().ToString("N") + "/attempt/0");
        using (var interruptedScope = fixture.Services.CreateScope())
        {
            var interruptedWork = interruptedScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var backup = new Backup(interruptedWork, first.ScheduledAt, requestedByUserId: first.RequestedByUserId, execution: first);
            interruptedWork.PreSaveChanges += () => throw new IOException("interrupted after archive save and before final commit");
            await Assert.ThrowsAsync<IOException>(() => interruptedWork.SaveChanges());
            Assert.Equal(first.BackupId, await backup.WaitForRecordAsync());
            Assert.Equal(BackupStatus.Running, (await Find(first.BackupId)).Status);
        }

        var partial = await Find(first.BackupId);
        var partialBlob = await DB.Collection<BlobObject>().Find(x => x.Id == partial.BlobObjectId).SingleAsync();
        var partialPath = work.Attach(partialBlob).GetFilePath();
        Assert.True(File.Exists(partialPath));
        var partialDirectory = Path.Combine(fixture.Options.WorkingDirectory, first.BackupId);
        Directory.CreateDirectory(partialDirectory);
        await File.WriteAllTextAsync(Path.Combine(partialDirectory, "partial"), "interrupted attempt");
        var replay = Worker();
        Assert.Equal(first.BackupId, await replay.TryStartManualWithIdAsync(first, CancellationToken.None));
        await replay.StopAsync(CancellationToken.None);
        var recovery = Worker();
        Assert.True(await recovery.TryRecoverManualAsync(first));
        var recovered = await Find(first.BackupId);
        Assert.Equal(BackupStatus.Failed, recovered.Status);
        Assert.Equal("Previous backup execution was interrupted.", recovered.ErrorMessage);
        Assert.Null(recovered.BlobObjectId);
        Assert.True(recovered.ResultNotificationSaved);
        Assert.False(File.Exists(partialPath));
        Assert.False(Directory.Exists(partialDirectory));
        var terminal = recovered.ToBson();
        Assert.True(await recovery.TryRecoverManualAsync(first));
        Assert.Equal(terminal, (await Find(first.BackupId)).ToBson());

        var second = new ManualBackupExecution(first.ExecutionKey.Replace("/attempt/0", "/attempt/1"),
            first.DatabaseName, first.RequestedByUserId, first.ScheduledAt.AddSeconds(1));
        _ids.Add(second.BackupId);
        Assert.Equal(second.BackupId, await recovery.TryStartManualWithIdAsync(second, CancellationToken.None));
        Assert.Equal(BackupStatus.Succeeded, (await WaitForCompletion(second.BackupId)).Status);
        await recovery.StopAsync(CancellationToken.None);
        Assert.Equal(terminal, (await Find(first.BackupId)).ToBson());

        foreach (var sentinel in sentinels)
        {
            Assert.Equal(sentinel.Snapshot, (await Find(sentinel.Backup.Id)).ToBson());
            Assert.True(File.Exists(sentinel.Path));
            Assert.Equal("existing backup work", await File.ReadAllTextAsync(Path.Combine(sentinel.Directory, "partial")));
            Assert.Equal(0L, await DB.Collection<Message>().CountDocumentsAsync(x => x.Id == sentinel.Backup.ResultMessageId));
            Assert.Equal(0L, await DB.Collection<MessageDistribution>().CountDocumentsAsync(x => x.ToUserId == sentinel.Backup.RequestedByUserId));
        }
    }

    [Theory]
    [InlineData("ExecutionKey")]
    [InlineData("RequestedByUserId")]
    [InlineData("DatabaseName")]
    [InlineData("ScheduledAt")]
    [InlineData("Source")]
    public async Task ConflictingIdentityCannotStartRecoverOrNotifyExistingBackup(string field)
    {
        var execution = Identity();
        var document = Sample(execution, BackupStatus.Running).ToBsonDocument();
        document[field] = field switch
        {
            "RequestedByUserId" => ObjectId.GenerateNewId().ToString(),
            "ScheduledAt" => new BsonDateTime(execution.ScheduledAt.AddMinutes(1).UtcDateTime),
            "Source" => "Automatic",
            _ => "existing-" + document[field].AsString
        };
        var existing = BsonSerializer.Deserialize<Backup>(document);
        await DB.Collection<Backup>().InsertOneAsync(existing);
        var before = (await Find(existing.Id)).ToBson();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new BackupsExecution(fixture.Services).TryStartManualWithIdAsync(execution, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new BackupsExecution(fixture.Services).TryRecoverManualAsync(execution));
        Assert.Equal(before, (await Find(existing.Id)).ToBson());
        Assert.Equal(0L, await DB.Collection<MessageDistribution>().CountDocumentsAsync(x => x.MessageId == existing.ResultMessageId));
    }

    [Fact]
    public async Task InsertionConflictDoesNotCompensateOrCleanTheExistingRecord()
    {
        var execution = Identity();
        var existing = Sample(execution, BackupStatus.Running, owned: false);
        await DB.Collection<Backup>().InsertOneAsync(existing);
        var before = (await Find(existing.Id)).ToBson();
        var directory = Path.Combine(fixture.Options.WorkingDirectory, existing.Id);
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "partial"), "existing work");
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        _ = new Backup(work, execution.ScheduledAt, requestedByUserId: execution.RequestedByUserId, execution: execution);
        await Assert.ThrowsAsync<MongoWriteException>(() => work.SaveChanges());
        Assert.Equal(before, (await Find(existing.Id)).ToBson());
        Assert.Equal("existing work", await File.ReadAllTextAsync(Path.Combine(directory, "partial")));
        Assert.Equal(0L, await DB.Collection<MessageDistribution>().CountDocumentsAsync(x => x.MessageId == existing.ResultMessageId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ArchiveOwnerIsSavedBeforeWritingAndUnboundRecoveryPreservesSameNameUserFiles(bool sharedContent)
    {
        var identity = Identity();
        var backup = Sample(identity, BackupStatus.Running);
        await DB.Collection<Backup>().InsertOneAsync(backup);
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var fileName = "backup-" + backup.Id + ".archive.gz";
        var ownerTag = Backup.StorageOwnerTag(backup);
        var bytes = new byte[] { 3, 1, 4, 1, 5 };
        var source = new GatedStream(bytes);
        var orphan = new BlobObject(new CreateBlobObjectRequest
        {
            File = new StreamFile(fileName, () => source), StorageType = BlobStorageType.FileSystem
        }, work, CancellationToken.None, ownerTag);
        _blobIds.Add(orphan.Id);
        try
        {
            await source.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var firstSave = await DB.Collection<BlobObject>().Find(x => x.Id == orphan.Id).SingleAsync();
            Assert.Equal(ownerTag, firstSave.StorageOwnerTag);
            Assert.True(firstSave.UploadPending);
            Assert.Null(firstSave.Md5);
            Assert.Null((await Find(backup.Id)).BlobObjectId);
        }
        finally
        {
            source.Release.TrySetResult();
            await orphan.WaitForStorageAsync();
        }
        await work.SaveChanges();
        var orphanPath = orphan.GetFilePath();
        Assert.True(File.Exists(orphanPath));

        var sentinels = new List<(BlobObject Blob, byte[] Snapshot, byte[] Content, string Path)>();
        foreach (var sample in new[]
        {
            (Owner: (string?)null, Content: sharedContent ? bytes : new byte[] { 8, 6, 7, 5 }),
            (Owner: (string?)"existing.document/" + Guid.NewGuid().ToString("N"), Content: new byte[] { 2, 7, 1, 8 })
        })
        {
            var blob = new BlobObject(new CreateBlobObjectRequest
            {
                File = new StreamFile(fileName, () => new MemoryStream(sample.Content)), StorageType = BlobStorageType.FileSystem
            }, work, CancellationToken.None, sample.Owner);
            _blobIds.Add(blob.Id);
            await work.SaveChanges();
            var saved = await DB.Collection<BlobObject>().Find(x => x.Id == blob.Id).SingleAsync();
            sentinels.Add((blob, saved.ToBson(), sample.Content, blob.GetFilePath()));
        }

        Assert.True(await Worker().TryRecoverManualAsync(identity));
        Assert.False(await DB.Collection<BlobObject>().Find(x => x.Id == orphan.Id).AnyAsync());
        Assert.False(await DB.Collection<BlobObject>().Find(x => x.StorageOwnerTag == ownerTag).AnyAsync());
        Assert.Equal(sharedContent, File.Exists(orphanPath));
        var recovered = await Find(backup.Id);
        Assert.Equal(BackupStatus.Failed, recovered.Status);
        Assert.Null(recovered.BlobObjectId);
        foreach (var sentinel in sentinels)
        {
            Assert.Equal(sentinel.Snapshot, (await DB.Collection<BlobObject>().Find(x => x.Id == sentinel.Blob.Id).SingleAsync()).ToBson());
            Assert.Equal(sentinel.Content, await File.ReadAllBytesAsync(sentinel.Path));
        }
    }

    [Fact]
    public async Task ExpirationRejectsAnExplicitBindingToAnotherStorageOwner()
    {
        var identity = Identity();
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var blob = new BlobObject(new CreateBlobObjectRequest
        {
            File = new StreamFile("backup-" + identity.BackupId + ".archive.gz", () => new MemoryStream([4, 2, 9])),
            StorageType = BlobStorageType.FileSystem
        }, work, CancellationToken.None, "existing.document/" + Guid.NewGuid().ToString("N"));
        _blobIds.Add(blob.Id);
        await work.SaveChanges();
        var snapshot = (await DB.Collection<BlobObject>().Find(x => x.Id == blob.Id).SingleAsync()).ToBson();
        var backup = Sample(identity, BackupStatus.Succeeded, blob.Id);
        await DB.Collection<Backup>().InsertOneAsync(backup);
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => work.Attach(backup).ExpireAsync());
        Assert.Contains("another storage owner", failure.Message);
        Assert.Equal(snapshot, (await DB.Collection<BlobObject>().Find(x => x.Id == blob.Id).SingleAsync()).ToBson());
        Assert.Equal(new byte[] { 4, 2, 9 }, await File.ReadAllBytesAsync(blob.GetFilePath()));
        var persisted = await Find(backup.Id);
        Assert.Equal(BackupStatus.Succeeded, persisted.Status);
        Assert.Equal(blob.Id, persisted.BlobObjectId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(300)]
    public async Task InjectedExporterKeepsAcceptedBackupRunningUntilActualCancellation(int cancellationDelayMilliseconds)
    {
        var identity = Identity();
        var exporter = new WaitingArchiveExporter();
        await using var services = fixture.CreateServices(exporter);
        var execution = new BackupsExecution(services);
        try
        {
            Assert.Equal(identity.BackupId, await execution.TryStartManualWithIdAsync(identity, CancellationToken.None));
            await exporter.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(cancellationDelayMilliseconds);
            Assert.Equal(BackupStatus.Running, (await Find(identity.BackupId)).Status);
            Assert.False(await DB.Collection<BlobObject>().Find(x => x.StorageOwnerTag == "geex.backup/" + identity.BackupId).AnyAsync());
            await execution.StopAsync(CancellationToken.None);
            var cancelled = await Find(identity.BackupId);
            Assert.Equal(BackupStatus.Cancelled, cancelled.Status);
            Assert.StartsWith("Archive export:", cancelled.ErrorMessage);
            Assert.Null(cancelled.BlobObjectId);
            Assert.True(cancelled.ResultNotificationSaved);
            var snapshot = cancelled.ToBson();
            var retry = Worker();
            Assert.Equal(identity.BackupId, await retry.TryStartManualWithIdAsync(identity, CancellationToken.None));
            await retry.StopAsync(CancellationToken.None);
            Assert.Equal(snapshot, (await Find(identity.BackupId)).ToBson());
            Assert.Equal(1L, await DB.Collection<MessageDistribution>().CountDocumentsAsync(x => x.MessageId == cancelled.ResultMessageId));
        }
        finally { await execution.StopAsync(CancellationToken.None); }
    }

    private sealed class WaitingArchiveExporter : IBackupArchiveExporter
    {
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task ExportAsync(string executable, string connectionString, string database, string directory, string archive,
            CancellationToken cancellationToken)
        {
            Entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }

    private sealed class GatedStream(byte[] bytes) : MemoryStream(bytes)
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return await base.ReadAsync(buffer, cancellationToken);
        }
    }

    private static Backup Sample(ManualBackupExecution execution, BackupStatus status, string? blobId = null, bool owned = true)
    {
        var document = new BsonDocument
        {
            { "_id", ObjectId.Parse(execution.BackupId) }, { "DatabaseName", execution.DatabaseName },
            { "ScheduledAt", new BsonDateTime(execution.ScheduledAt.UtcDateTime) },
            { "StartedAt", new BsonDateTime(execution.ScheduledAt.UtcDateTime) },
            { "Source", "Manual" }, { "Status", status.ToString() },
            { "RequestedByUserId", execution.RequestedByUserId }, { "ResultMessageId", ObjectId.GenerateNewId().ToString() }
        };
        if (owned) document["ExecutionKey"] = execution.ExecutionKey;
        if (blobId != null) document["BlobObjectId"] = blobId;
        return BsonSerializer.Deserialize<Backup>(document);
    }

    private static Task<Backup> Find(string id) => DB.Collection<Backup>().Find(x => x.Id == id).SingleAsync();

    private async Task<Backup> WaitForCompletion(string id)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (true)
        {
            var backup = await DB.Collection<Backup>().Find(x => x.Id == id).SingleAsync(deadline.Token);
            var active = await DB.DefaultDb.GetCollection<BsonDocument>("_geex_database_backup_leases").Find(
                new BsonDocument { { "_id", fixture.DatabaseName }, { "ExpiresAt", new BsonDocument("$gt", DateTime.UtcNow) } }).AnyAsync(deadline.Token);
            if (backup.Status != BackupStatus.Running && backup.ResultNotificationSaved && !active) return backup;
            await Task.Delay(20, deadline.Token);
        }
    }
}

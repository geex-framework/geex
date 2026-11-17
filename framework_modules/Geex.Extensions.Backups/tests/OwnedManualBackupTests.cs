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
public sealed class OwnedManualBackupTests(BackupFixture fixture) : IAsyncLifetime
{
    private static long _slot;
    private readonly List<string> _ids = [];
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
        var blobIds = backups.Where(x => x.BlobObjectId != null).Select(x => x.BlobObjectId!).ToArray();
        var blobs = await DB.Collection<BlobObject>().Find(Builders<BlobObject>.Filter.In(x => x.Id, blobIds)).ToListAsync();
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
            var accepted = await Task.WhenAll(workers.Select(x => x.TryStartOwnedManualAsync(execution)));
            Assert.Contains(execution.BackupId, accepted);
            Assert.All(accepted.Where(x => x != null), id => Assert.Equal(execution.BackupId, id));
            var completed = await WaitForCompletion(execution.BackupId);
            Assert.True(execution.Matches(completed));
            Assert.Equal(BackupStatus.Succeeded, completed.Status);
            Assert.True(completed.CreatedOn > DateTimeOffset.MinValue);
            var snapshot = completed.ToBson();
            var retry = Worker();
            Assert.Equal(execution.BackupId, await retry.TryStartOwnedManualAsync(execution));
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
        Assert.Equal(first.BackupId, await replay.TryStartOwnedManualAsync(first));
        await replay.StopAsync(CancellationToken.None);
        var recovery = Worker();
        Assert.True(await recovery.TryRecoverOwnedManualAsync(first));
        var recovered = await Find(first.BackupId);
        Assert.Equal(BackupStatus.Failed, recovered.Status);
        Assert.Equal("Previous backup execution was interrupted.", recovered.ErrorMessage);
        Assert.Null(recovered.BlobObjectId);
        Assert.True(recovered.ResultNotificationSaved);
        Assert.False(File.Exists(partialPath));
        Assert.False(Directory.Exists(partialDirectory));
        var terminal = recovered.ToBson();
        Assert.True(await recovery.TryRecoverOwnedManualAsync(first));
        Assert.Equal(terminal, (await Find(first.BackupId)).ToBson());

        var second = new ManualBackupExecution(first.ExecutionKey.Replace("/attempt/0", "/attempt/1"),
            first.DatabaseName, first.RequestedByUserId, first.ScheduledAt.AddSeconds(1));
        _ids.Add(second.BackupId);
        Assert.Equal(second.BackupId, await recovery.TryStartOwnedManualAsync(second));
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
        await Assert.ThrowsAsync<InvalidOperationException>(() => new BackupsExecution(fixture.Services).TryStartOwnedManualAsync(execution));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new BackupsExecution(fixture.Services).TryRecoverOwnedManualAsync(execution));
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

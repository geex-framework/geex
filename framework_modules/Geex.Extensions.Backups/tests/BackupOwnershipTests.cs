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
public sealed class BackupOwnershipTests(BackupFixture fixture) : IAsyncLifetime
{
    private readonly List<string> _ids = [];
    private readonly List<string> _blobIds = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ArchiveOwnerIsSavedBeforeWritingAndUnboundRecoveryPreservesSameNameUserFiles(bool sharedContent)
    {
        var backup = Sample(BackupStatus.Running);
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

        await Backup.RecoverAsync(fixture.Services, fixture.DatabaseName, CancellationToken.None);
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
        var backup = Sample(BackupStatus.Succeeded);
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var blob = new BlobObject(new CreateBlobObjectRequest
        {
            File = new StreamFile("backup-" + backup.Id + ".archive.gz", () => new MemoryStream([4, 2, 9])),
            StorageType = BlobStorageType.FileSystem
        }, work, CancellationToken.None, "existing.document/" + Guid.NewGuid().ToString("N"));
        _blobIds.Add(blob.Id);
        await work.SaveChanges();
        var snapshot = (await DB.Collection<BlobObject>().Find(x => x.Id == blob.Id).SingleAsync()).ToBson();
        var document = backup.ToBsonDocument();
        document["BlobObjectId"] = blob.Id;
        backup = BsonSerializer.Deserialize<Backup>(document);
        await DB.Collection<Backup>().InsertOneAsync(backup);
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => work.Attach(backup).ExpireAsync());
        Assert.Contains("another storage owner", failure.Message);
        Assert.Equal(snapshot, (await DB.Collection<BlobObject>().Find(x => x.Id == blob.Id).SingleAsync()).ToBson());
        Assert.Equal(new byte[] { 4, 2, 9 }, await File.ReadAllBytesAsync(blob.GetFilePath()));
        var persisted = await Find(backup.Id);
        Assert.Equal(BackupStatus.Succeeded, persisted.Status);
        Assert.Equal(blob.Id, persisted.BlobObjectId);
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

    private Backup Sample(BackupStatus status)
    {
        var id = ObjectId.GenerateNewId();
        _ids.Add(id.ToString());
        return BsonSerializer.Deserialize<Backup>(new BsonDocument
        {
            { "_id", id }, { "DatabaseName", fixture.DatabaseName },
            { "ScheduledAt", new BsonDateTime(DateTime.UtcNow) }, { "StartedAt", new BsonDateTime(DateTime.UtcNow) },
            { "Source", "Manual" }, { "Status", status.ToString() },
            { "RequestedByUserId", ObjectId.GenerateNewId().ToString() }, { "ResultMessageId", ObjectId.GenerateNewId().ToString() }
        });
    }

    private static Task<Backup> Find(string id) => DB.Collection<Backup>().Find(x => x.Id == id).SingleAsync();
}

using System.Text.Json;
using Geex.Extensions.Backups.Core.Entities;
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
public sealed class BackupArchiveCleanupTests(BackupFixture fixture) : IAsyncLifetime
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
        var blobs = await DB.Collection<BlobObject>().Find(Builders<BlobObject>.Filter.In(x => x.Id, blobIds)).ToListAsync();
        foreach (var blob in blobs) await work.Attach(blob).DeleteAsync();
        await work.SaveChanges();
        var messageIds = backups.Select(x => x.ResultMessageId).ToArray();
        await DB.Collection<MessageDistribution>().DeleteManyAsync(Builders<MessageDistribution>.Filter.In(x => x.MessageId, messageIds));
        await DB.Collection<Message>().DeleteManyAsync(Builders<Message>.Filter.In(x => x.Id, messageIds));
        await DB.Collection<Backup>().DeleteManyAsync(Builders<Backup>.Filter.In(x => x.Id, _ids));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CleanupDeletesBoundArchivesAndPreservesUnboundAndSameNameFiles(bool sharedContent)
    {
        var backup = Sample();
        var unboundBackup = Sample();
        await DB.Collection<Backup>().InsertManyAsync([backup, unboundBackup]);
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var fileName = "backup-" + backup.Id + ".archive.gz";
        var bytes = new byte[] { 3, 1, 4, 1, 5 };
        var archive = new BlobObject(new CreateBlobObjectRequest
        {
            File = new StreamFile(fileName, () => new MemoryStream(bytes)), StorageType = BlobStorageType.FileSystem
        }, work, CancellationToken.None);
        _blobIds.Add(archive.Id);
        await work.SaveChanges();
        await DB.Collection<Backup>().UpdateOneAsync(x => x.Id == backup.Id,
            Builders<Backup>.Update.Set(x => x.BlobObjectId, archive.Id));
        var archivePath = archive.GetFilePath();
        Assert.True(File.Exists(archivePath));

        var sentinels = new List<(BlobObject Blob, byte[] Snapshot, byte[] Content, string Path)>();
        foreach (var sample in new[]
        {
            (Name: "backup-" + unboundBackup.Id + ".archive.gz", Content: new byte[] { 2, 7, 1, 8 }),
            (Name: fileName, Content: sharedContent ? bytes : new byte[] { 8, 6, 7, 5 })
        })
        {
            var blob = new BlobObject(new CreateBlobObjectRequest
            {
                File = new StreamFile(sample.Name, () => new MemoryStream(sample.Content)), StorageType = BlobStorageType.FileSystem
            }, work, CancellationToken.None);
            _blobIds.Add(blob.Id);
            await work.SaveChanges();
            var saved = await DB.Collection<BlobObject>().Find(x => x.Id == blob.Id).SingleAsync();
            sentinels.Add((blob, saved.ToBson(), sample.Content, blob.GetFilePath()));
        }

        await Backup.CleanupFailedAsync(fixture.Services, fixture.DatabaseName, CancellationToken.None);
        Assert.False(await DB.Collection<BlobObject>().Find(x => x.Id == archive.Id).AnyAsync());
        Assert.Equal(sharedContent, File.Exists(archivePath));
        foreach (var id in new[] { backup.Id, unboundBackup.Id })
        {
            var stored = await DB.Collection<Backup>().Find(x => x.Id == id).SingleAsync();
            Assert.Equal(BackupStatus.Failed, stored.Status);
            Assert.Null(stored.BlobObjectId);
        }
        foreach (var sentinel in sentinels)
        {
            Assert.Equal(sentinel.Snapshot, (await DB.Collection<BlobObject>().Find(x => x.Id == sentinel.Blob.Id).SingleAsync()).ToBson());
            Assert.Equal(sentinel.Content, await File.ReadAllBytesAsync(sentinel.Path));
        }

        var evidence = Environment.GetEnvironmentVariable("GEEX_BACKUP_TEST_EVIDENCE");
        if (!string.IsNullOrWhiteSpace(evidence))
        {
            var cases = Path.Combine(evidence, "cases");
            Directory.CreateDirectory(cases);
            await File.WriteAllTextAsync(Path.Combine(cases, $"archive-association-cleanup-{sharedContent}.json"),
                JsonSerializer.Serialize(new { observations = new[] { new {
                    backupId = backup.Id, deletedBlobId = archive.Id,
                    unboundBackupId = unboundBackup.Id, preservedUnboundBlobId = sentinels[0].Blob.Id,
                    preservedSameNameBlobId = sentinels[1].Blob.Id, sharedContent,
                    boundBlobDeleted = true, unboundBlobAndFilePreserved = true, sameNameBlobAndFilePreserved = true
                } } }));
        }
    }

    private Backup Sample()
    {
        var id = ObjectId.GenerateNewId();
        _ids.Add(id.ToString());
        return BsonSerializer.Deserialize<Backup>(new BsonDocument
        {
            { "_id", id }, { "DatabaseName", fixture.DatabaseName },
            { "ScheduledAt", new BsonDateTime(BackupIntegrationTests.NextSlot().UtcDateTime) }, { "StartedAt", new BsonDateTime(DateTime.UtcNow) },
            { "Source", "Manual" }, { "Status", BackupStatus.Failed.ToString() },
            { "RequestedByUserId", ObjectId.GenerateNewId().ToString() }, { "ResultMessageId", ObjectId.GenerateNewId().ToString() }
        });
    }
}

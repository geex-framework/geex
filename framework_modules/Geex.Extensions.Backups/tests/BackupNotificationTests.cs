using Geex.Extensions.Backups.Core.Entities;
using Geex.Extensions.Backups.Core.Jobs;
using Geex.Extensions.Messaging.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Entities;
using Xunit;
using Geex.Storage;
using Geex.Extensions.BlobStorage;
using HotChocolate.Subscriptions;

namespace Geex.Extensions.Backups.Tests;

[Collection("Backups")]
public class BackupNotificationTests(BackupFixture fixture)
{
    [Theory]
    [InlineData(BackupStatus.Succeeded)]
    [InlineData(BackupStatus.Failed)]
    [InlineData(BackupStatus.Cancelled)]
    public async Task TerminalResultsAreDirectedIdempotentAndPreserveReadState(BackupStatus status)
    {
        var id = ObjectId.GenerateNewId().ToString();
        var messageId = ObjectId.GenerateNewId().ToString();
        var userId = ObjectId.GenerateNewId().ToString();
        var finished = new DateTimeOffset(2026, 9, 22, 14, 15, 16, TimeSpan.Zero);
        var backup = BsonSerializer.Deserialize<Backup>(new BsonDocument {
            { "_id", ObjectId.Parse(id) }, { "Source", "Manual" }, { "Status", status.ToString() },
            { "RequestedByUserId", userId }, { "ResultMessageId", messageId },
            { "DatabaseName", fixture.DatabaseName + id }, { "FinishedAt", new BsonDateTime(finished.UtcDateTime) }
        });
        await DB.Collection<Backup>().InsertOneAsync(backup);
        await Backup.SaveResultMessageAsync(fixture.Services, id);
        var message = await DB.Collection<Message>().Find(x => x.Id == messageId).SingleAsync();
        Assert.Equal(finished, message.CreatedOn);
        Assert.Equal(finished, message.ModifiedOn);
        Assert.Equal(status.ToString(), message.Meta!["status"]!.GetValue<string>());
        Assert.Equal("backups.view", message.Meta["actions"]![0]!["key"]!.GetValue<string>());
        var delivery = await DB.Collection<MessageDistribution>().Find(x => x.MessageId == messageId).SingleAsync();
        Assert.Equal(userId, delivery.ToUserId);
        Assert.Equal(finished, delivery.CreatedOn);
        await DB.Collection<MessageDistribution>().UpdateOneAsync(x => x.Id == delivery.Id,
            Builders<MessageDistribution>.Update.Set(x => x.IsRead, true));
        // Simulate interruption between message persistence and the backup marker.
        await DB.Collection<Backup>().UpdateOneAsync(x => x.Id == id, Builders<Backup>.Update.Set(x => x.ResultNotificationSaved, false));
        await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => Backup.SaveResultMessageAsync(fixture.Services, id)));
        await Backup.RecoverNotificationsAsync(fixture.Services);
        Assert.Equal(1, await DB.Collection<Message>().CountDocumentsAsync(x => x.Id == messageId));
        Assert.Equal(1, await DB.Collection<MessageDistribution>().CountDocumentsAsync(x => x.MessageId == messageId));
        Assert.True((await DB.Collection<MessageDistribution>().Find(x => x.Id == delivery.Id).SingleAsync()).IsRead);
        Assert.True((await DB.Collection<Backup>().Find(x => x.Id == id).SingleAsync()).ResultNotificationSaved);
        await DB.Collection<Message>().UpdateOneAsync(x => x.Id == messageId,
            Builders<Message>.Update.Set(x => x.CreatedOn, DateTimeOffset.MinValue).Set(x => x.ModifiedOn, DateTimeOffset.MinValue));
        await DB.Collection<MessageDistribution>().UpdateOneAsync(x => x.Id == delivery.Id,
            Builders<MessageDistribution>.Update.Set(x => x.CreatedOn, DateTimeOffset.MinValue));
        await Backup.RecoverNotificationsAsync(fixture.Services);
        Assert.Equal(finished, (await DB.Collection<Message>().Find(x => x.Id == messageId).SingleAsync()).CreatedOn);
        var repaired = await DB.Collection<MessageDistribution>().Find(x => x.Id == delivery.Id).SingleAsync();
        Assert.True(repaired.IsRead);
        Assert.Equal(finished, repaired.CreatedOn);
        await Backup.RecoverNotificationsAsync(fixture.Services);
        Assert.Equal(1, await DB.Collection<MessageDistribution>().CountDocumentsAsync(x => x.MessageId == messageId));
    }

    [Theory]
    [InlineData("Automatic", true)]
    [InlineData("Manual", false)]
    [InlineData("Unknown", false)]
    public async Task AutomaticAndHistoricalResultsNeverBroadcast(string source, bool requester)
    {
        var id = ObjectId.GenerateNewId().ToString();
        var messageId = ObjectId.GenerateNewId().ToString();
        var document = new BsonDocument { { "_id", ObjectId.Parse(id) }, { "Source", source },
            { "Status", "Succeeded" }, { "DatabaseName", fixture.DatabaseName + id } };
        if (requester) { document["RequestedByUserId"] = ObjectId.GenerateNewId().ToString(); document["ResultMessageId"] = messageId; }
        await DB.Collection<Backup>().InsertOneAsync(BsonSerializer.Deserialize<Backup>(document));
        await Backup.SaveResultMessageAsync(fixture.Services, id);
        Assert.Equal(0, await DB.Collection<Message>().CountDocumentsAsync(x => x.Id == messageId));
        await DB.Collection<Backup>().DeleteOneAsync(x => x.Id == id);
    }

    [Fact]
    public async Task ManualAcceptancePersistsRequesterBeforeReturning()
    {
        var execution = new BackupsExecution(fixture.Services);
        var user = ObjectId.GenerateNewId().ToString();
        var id = await execution.TryStartManualWithIdAsync(user);
        Assert.NotNull(id);
        var accepted = await DB.Collection<Backup>().Find(x => x.Id == id).SingleAsync();
        Assert.Equal(user, accepted.RequestedByUserId);
        Assert.NotNull(accepted.ResultMessageId);
        await execution.StopAsync(CancellationToken.None);
        var finished = await DB.Collection<Backup>().Find(x => x.Id == id).SingleAsync();
        Assert.NotEqual(BackupStatus.Running, finished.Status);
        Assert.True(finished.ResultNotificationSaved);
    }
    [Fact]
    public async Task BrokenNotificationsNeverChangeBackupResultOrDeleteArchive()
    {
        var sender = new BrokenSender();
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton(fixture.Options)
            .AddSingleton(new GeexCoreModuleOptions { ConnectionString = fixture.Uri })
            .AddSingleton(new BlobStorageModuleOptions { FileSystemStoragePath = Path.Combine(fixture.Root, "blobs") })
            .AddSingleton<ITopicEventSender>(sender)
            .AddScoped<IUnitOfWork>(sp => new GeexDbContext(sp)).BuildServiceProvider();
        using var scope = services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backup = new Backup(work, DateTimeOffset.UtcNow.AddYears(1), requestedByUserId: ObjectId.GenerateNewId().ToString());
        await work.SaveChanges();
        var archive = backup.File!.GetFilePath();
        await Backup.PublishChangedAsync(services, backup.Id);
        await Backup.SaveResultMessageAsync(services, backup.Id);
        Assert.True(sender.Attempts >= 2);
        var saved = await DB.Collection<Backup>().Find(x => x.Id == backup.Id).SingleAsync();
        Assert.Equal(BackupStatus.Succeeded, saved.Status);
        Assert.Equal(backup.BlobObjectId, saved.BlobObjectId);
        Assert.True(saved.ResultNotificationSaved);
        Assert.True(File.Exists(archive));
        Assert.Equal(1, await DB.Collection<MessageDistribution>().CountDocumentsAsync(x => x.MessageId == saved.ResultMessageId));
    }

    [Fact]
    public async Task CleanupPublishesPersistedFailureAndCompletedState()
    {
        var sender = new StateRecordingSender();
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton(fixture.Options)
            .AddSingleton(new GeexCoreModuleOptions { ConnectionString = fixture.Uri })
            .AddSingleton(new BlobStorageModuleOptions { FileSystemStoragePath = Path.Combine(fixture.Root, "blobs") })
            .AddSingleton<ITopicEventSender>(sender)
            .AddScoped<IUnitOfWork>(sp => new GeexDbContext(sp)).BuildServiceProvider();
        using var scope = services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backup = new Backup(work, DateTimeOffset.UtcNow.AddYears(2));
        await work.SaveChanges();
        sender.States.Clear();
        var blob = backup.File!;
        var blockedPath = Path.Combine(Path.GetDirectoryName(blob.GetFilePath())!, blob.Id + ".upload");
        Directory.CreateDirectory(blockedPath);
        try
        {
            await Assert.ThrowsAnyAsync<Exception>(() => backup.ExpireAsync());
            var failed = Assert.Single(sender.States);
            Assert.Equal(backup.Id, failed.Id);
            Assert.Equal(BackupStatus.Succeeded, failed.Status);
            Assert.StartsWith("Archive cleanup:", failed.Error);
        }
        finally { Directory.Delete(blockedPath); }
        await backup.ExpireAsync();
        Assert.Equal(2, sender.States.Count);
        Assert.Equal(BackupStatus.Expired, sender.States[1].Status);
        Assert.Null(sender.States[1].Error);
    }

    private sealed class StateRecordingSender : ITopicEventSender
    {
        public List<(string Id, BackupStatus Status, string? Error)> States { get; } = [];

        public async ValueTask SendAsync<T>(string topicName, T message, CancellationToken cancellationToken = default)
        {
            if (topicName == Backup.ChangedTopic && message is string id)
            {
                var stored = await DB.Collection<Backup>().Find(x => x.Id == id).SingleAsync(cancellationToken);
                States.Add((id, stored.Status, stored.ErrorMessage));
            }
        }

        public ValueTask CompleteAsync(string topicName) => ValueTask.CompletedTask;
    }

    private sealed class BrokenSender : ITopicEventSender
    {
        public int Attempts;
        public ValueTask SendAsync<T>(string topicName, T message, CancellationToken cancellationToken = default)
        { Attempts++; throw new IOException("Redis unavailable"); }
        public ValueTask CompleteAsync(string topicName) => throw new IOException("Redis unavailable");
    }

}

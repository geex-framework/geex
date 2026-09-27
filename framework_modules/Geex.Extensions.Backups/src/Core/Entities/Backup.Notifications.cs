using System.Text.Json.Nodes;
using Geex.Extensions.Messaging;
using Geex.Extensions.Messaging.Core.Entities;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Geex.Extensions.Backups.Core.Entities;

public partial class Backup
{
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

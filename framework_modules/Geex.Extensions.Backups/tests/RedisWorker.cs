using System.Diagnostics;
using System.Runtime.CompilerServices;
using Geex.ClientNotification;
using Geex.Storage;
using MongoDB.Driver;
using MongoDB.Entities;
using System.Net;
using System.Net.Sockets;
using Geex.Extensions.Messaging.ClientNotification;
using Geex.Extensions.Messaging.Core.Entities;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;
using StackExchange.Redis.Extensions.Core.Configuration;
using Xunit;

namespace Geex.Extensions.Backups.Tests;

public static class RedisWorker
{
    public static async Task Main(string[] args)
    {
        if (args.Length == 0) return;
        var config = new GeexCoreModuleOptions { AppName = "cross-process", ConnectionString = args[3],
            Redis = new RedisConfiguration { Hosts = [new RedisHost { Host = "127.0.0.1", Port = int.Parse(args[1]) }], Database = 1, PoolSize = 1 } };
        var services = new ServiceCollection().AddLogging().AddSingleton(config);
        var settings = MongoClientSettings.FromConnectionString(config.ConnectionString);
        settings.LinqProvider = global::MongoDB.Driver.Linq.LinqProvider.V2;
        var database = new MongoUrl(config.ConnectionString).DatabaseName;
        await DB.InitAsync(database, settings); DB.ChangeDefaultDatabase(database);
        RuntimeHelpers.RunClassConstructor(typeof(GeexCoreModule).TypeHandle);
        var mapping = new Message.MessageBsonConfig();
        mapping.GetType().GetInterface("Geex.IBsonConfig")!.GetMethod("Map")!.Invoke(mapping, null);
        services.AddScoped<IUnitOfWork>(sp => new GeexDbContext(sp));
        services.AddStackExchangeRedisExtensions();
        services.AddGraphQLServer().AddRedisSubscriptions(sp => sp.GetRequiredService<IRedisConnectionPoolManager>().GetConnection(),
            new SubscriptionOptions { TopicPrefix = GeexSubscriptionTopics.Prefix(config, "Test") });
        await using var provider = services.BuildServiceProvider();
        var pool = provider.GetRequiredService<IRedisConnectionPoolManager>();
        var sharedConnection = pool.GetConnection();
        var cache = provider.GetRequiredService<IRedisDatabase>();
        var sender = provider.GetRequiredService<ITopicEventSender>();
        var receiver = provider.GetRequiredService<ITopicEventReceiver>();
        if (args[0] == "receive")
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var stream = new ClientNotifySubscription().OnPublicNotify(receiver,
                provider.GetRequiredService<IServiceScopeFactory>(), timeout.Token);
            await using (var events = stream.GetAsyncEnumerator(timeout.Token))
            {
                Assert.True(await events.MoveNextAsync());
                Assert.IsType<SubscriptionReadyClientNotify>(events.Current);
                await File.WriteAllTextAsync(args[2], "ready");
                Assert.True(await events.MoveNextAsync());
                Assert.Equal(DataChangeType.User, Assert.IsType<DataChangeClientNotify>(events.Current).DataChangeType);
                Assert.True(await events.MoveNextAsync());
                var notification = Assert.IsType<NewMessageClientNotify>(events.Current);
                Assert.Equal("000000000000000000000123", notification.Message.Id);
                Assert.Equal("Persisted message from another process", notification.Message.Title);
            }
            await cache.AddAsync("after-stream", "alive");
            Assert.Equal("alive", await cache.GetAsync<string>("after-stream"));
            Assert.True(sharedConnection.IsConnected);
        }
        else
        {
            // A different application publishes first: the receiver must not observe it.
            var otherServices = new ServiceCollection().AddLogging();
            otherServices.AddGraphQLServer().AddRedisSubscriptions(_ => sharedConnection,
                new SubscriptionOptions { TopicPrefix = GeexSubscriptionTopics.Prefix(new GeexCoreModuleOptions {
                    AppName = "other-app", ConnectionString = config.ConnectionString, Redis = config.Redis }, "Test") });
            await using (var other = otherServices.BuildServiceProvider())
                await other.GetRequiredService<ITopicEventSender>().SendAsync(nameof(ClientNotifySubscription.OnPublicNotify), ClientNotifyEnvelope.From(new DataChangeClientNotify(DataChangeType.Org)));
            Assert.True(sharedConnection.IsConnected);
            await sender.SendAsync(nameof(ClientNotifySubscription.OnPublicNotify), ClientNotifyEnvelope.From(new DataChangeClientNotify(DataChangeType.User)));
#pragma warning disable CS0618
            var message = new Message("test", Geex.Extensions.Messaging.MessageSeverityType.Success, null) { Id = "000000000000000000000123" };
#pragma warning restore CS0618
            await sender.SendAsync(nameof(ClientNotifySubscription.OnPublicNotify), ClientNotifyEnvelope.From(new NewMessageClientNotify(message)));
        }
        await File.WriteAllTextAsync(args[2] + ".done", "passed");
    }
}

[Collection("Backups")]
public class RedisSubscriptionTests(BackupFixture fixture)
{
    [Fact]
    public async Task IndependentProcessesShareNotificationsAndKeepCacheConnectionAlive()
    {
#pragma warning disable CS0618
        await DB.Collection<Message>().InsertOneAsync(new Message("Persisted message from another process",
            Geex.Extensions.Messaging.MessageSeverityType.Success, null) { Id = "000000000000000000000123" });
#pragma warning restore CS0618
        var root = Path.Combine(Environment.GetEnvironmentVariable("GEEX_BACKUP_TEST_EVIDENCE") ??
            Path.Combine(AppContext.BaseDirectory, ".test-evidence", "database-backup"), "redis-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        using var portFinder = new TcpListener(IPAddress.Loopback, 0);
        portFinder.Start(); var port = ((IPEndPoint)portFinder.LocalEndpoint).Port; portFinder.Stop();
        using var redis = Start("redis-server", root, "--bind", "127.0.0.1", "--port", port.ToString(), "--dir", root, "--logfile", Path.Combine(root, "redis.log"));
        Process? receiver = null;
        try
        {
            var ready = Path.Combine(root, "receiver");
            receiver = Start("dotnet", root, typeof(RedisWorker).Assembly.Location, "receive", port.ToString(), ready, fixture.Uri);
            var receiveOutput = receiver.StandardOutput.ReadToEndAsync(); var receiveError = receiver.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while (!File.Exists(ready) && !receiver.HasExited) await Task.Delay(50, timeout.Token);
            Assert.True(File.Exists(ready), await FinishedError(receiver, receiveError));
            using var sender = Start("dotnet", root, typeof(RedisWorker).Assembly.Location, "send", port.ToString(), Path.Combine(root, "sender"), fixture.Uri);
            var sendOutput = sender.StandardOutput.ReadToEndAsync(); var sendError = sender.StandardError.ReadToEndAsync();
            await sender.WaitForExitAsync(timeout.Token);
            await File.WriteAllTextAsync(Path.Combine(root, "sender.log"), await sendOutput + await sendError);
            Assert.Equal(0, sender.ExitCode);
            await receiver.WaitForExitAsync(timeout.Token);
            await File.WriteAllTextAsync(Path.Combine(root, "receiver.log"), await receiveOutput + await receiveError);
            Assert.Equal(0, receiver.ExitCode);
            Assert.True(File.Exists(ready + ".done"));
        }
        finally
        {
            if (receiver != null) { if (!receiver.HasExited) receiver.Kill(true); receiver.Dispose(); }
            if (!redis.HasExited) { redis.Kill(true); await redis.WaitForExitAsync(); }
        }
    }
    private static async Task<string> FinishedError(Process process, Task<string> error) => process.HasExited ? await error : "Receiver not ready.";
    private static Process Start(string name, string root, params string[] args)
    {
        var info = new ProcessStartInfo(name) { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in args) info.ArgumentList.Add(arg);
        return Process.Start(info)!;
    }
}

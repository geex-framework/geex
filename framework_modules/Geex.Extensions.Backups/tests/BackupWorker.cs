using System.Runtime.CompilerServices;
using System.Text.Json;
using Geex.Extensions.Backups.Core.Entities;
using Geex.Extensions.Backups.Core.Jobs;
using Geex.Extensions.BlobStorage;
using Geex.Extensions.BlobStorage.Core.Entities;
using Geex.Extensions.Messaging.Core.Entities;
using Geex.Storage;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Entities;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Configuration;

namespace Geex.Extensions.Backups.Tests;

internal static class BackupWorker
{
    internal static async Task RunAsync(string[] args)
    {
        var config = new GeexCoreModuleOptions { ConnectionString = args[3], Redis = new RedisConfiguration {
            Hosts = [new RedisHost { Host = "127.0.0.1", Port = int.Parse(args[1]) }], Database = 1, PoolSize = 1 } };
        var settings = MongoClientSettings.FromConnectionString(config.ConnectionString);
        settings.LinqProvider = global::MongoDB.Driver.Linq.LinqProvider.V2;
        var database = new MongoUrl(config.ConnectionString).DatabaseName;
        await DB.InitAsync(database, settings);
        DB.ChangeDefaultDatabase(database);
        RuntimeHelpers.RunClassConstructor(typeof(GeexCoreModule).TypeHandle);
        foreach (var mapping in new object[] { new Backup.BackupBsonConfig(), new BlobObject.BlobObjectBsonConfig(), new Message.MessageBsonConfig() })
            mapping.GetType().GetInterface("Geex.IBsonConfig")!.GetMethod("Map")!.Invoke(mapping, null);
        var root = args[2] + ".files";
        Directory.CreateDirectory(root);
        var port = int.Parse(args[5]);
        if (port != 0) config.ConnectionString = $"mongodb://127.0.0.1:{port}/{database}";
        await using var services = new ServiceCollection().AddLogging().AddSingleton(config).AddStackExchangeRedisExtensions()
            .AddSingleton(new BackupsModuleOptions { WorkingDirectory = Path.Combine(root, "backup"),
                MongodumpPath = Environment.GetEnvironmentVariable("GEEX_BACKUP_MONGODUMP") ?? "mongodump" })
            .AddSingleton(new BlobStorageModuleOptions { FileSystemStoragePath = Path.Combine(root, "blobs") })
            .AddScoped<IUnitOfWork>(sp => new GeexDbContext(sp)).BuildServiceProvider();
        var execution = new BackupsExecution(services);
        try
        {
            var id = await execution.TryStartManualWithIdAsync(force: args[4] == "force");
            await File.WriteAllTextAsync(args[2], JsonSerializer.Serialize(new { id }));
            Backup? result = null;
            if (id != null)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                do
                {
                    await Task.Delay(20, timeout.Token);
                    result = await DB.Collection<Backup>().Find(x => x.Id == id).SingleAsync(timeout.Token);
                } while (result.Status == BackupStatus.Running);
            }
            await execution.StopAsync(CancellationToken.None);
            await File.WriteAllTextAsync(args[2] + ".done", JsonSerializer.Serialize(new { id, status = result?.Status.ToString() }));
        }
        finally { await execution.StopAsync(CancellationToken.None); }
    }
}

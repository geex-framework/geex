using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Geex.Extensions.BlobStorage;
using Geex.Extensions.BlobStorage.Core.Entities;
using Geex.Extensions.Backups.Core.Entities;
using Geex.Extensions.Backups.Core;
using Geex.Storage;
using Geex.Extensions.Messaging.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;
using Xunit;

namespace Geex.Extensions.Backups.Tests;

public sealed class BackupFixture : IAsyncLifetime
{
    private Process? _mongo;
    private bool _externalMongo;
    private readonly List<string> _additionalDatabases = [];
    public string Root { get; } = Path.Combine(Environment.GetEnvironmentVariable("GEEX_BACKUP_TEST_EVIDENCE") ?? Path.Combine(AppContext.BaseDirectory, ".test-evidence", "database-backup"), "geex-backup-tests-" + Guid.NewGuid().ToString("N"));
    public string DatabaseName { get; } = "geex_backup_test_" + Guid.NewGuid().ToString("N");
    public string Uri { get; private set; } = "";
    public ServiceProvider Services { get; private set; } = null!;
    public BackupsModuleOptions Options { get; private set; } = null!;

    public string AllocateDatabaseName(string purpose)
    {
        if (purpose.Any(character => !char.IsAsciiLetter(character) && character != '_'))
            throw new ArgumentException("Database purpose must contain ASCII letters or underscores.", nameof(purpose));
        var name = "geex_backup_test_" + purpose + "_" + Guid.NewGuid().ToString("N");
        _additionalDatabases.Add(name);
        return name;
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Path.Combine(Root, "mongo"));
        Directory.CreateDirectory(Path.Combine(Root, "blobs"));
        var externalMongo = Environment.GetEnvironmentVariable("GEEX_BACKUP_TEST_MONGO_URI");
        if (!string.IsNullOrWhiteSpace(externalMongo))
        {
            var supplied = new MongoUrlBuilder(externalMongo);
            supplied.DatabaseName = DatabaseName;
            Uri = supplied.ToString();
            _externalMongo = true;
        }
        else
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            var info = new ProcessStartInfo(Environment.GetEnvironmentVariable("GEEX_BACKUP_MONGOD") ?? "mongod")
            { UseShellExecute = false, CreateNoWindow = true };
            foreach (var arg in new[] { "--bind_ip", "127.0.0.1", "--port", port.ToString(), "--dbpath", Path.Combine(Root, "mongo"), "--logpath", Path.Combine(Root, "mongo.log"), "--setParameter", "enableTestCommands=1" })
                info.ArgumentList.Add(arg);
            _mongo = Process.Start(info) ?? throw new InvalidOperationException("Could not start isolated mongod.");
            Uri = $"mongodb://127.0.0.1:{port}/{DatabaseName}";
        }
        var settings = MongoClientSettings.FromConnectionString(Uri);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
        settings.MaxConnectionPoolSize = 4;
        settings.LinqProvider = global::MongoDB.Driver.Linq.LinqProvider.V2;
        await DB.InitAsync(DatabaseName, settings);
        DB.ChangeDefaultDatabase(DatabaseName);
        RuntimeHelpers.RunClassConstructor(typeof(GeexCoreModule).TypeHandle);
        ConfigureMap(new Backup.BackupBsonConfig());
        ConfigureMap(new BlobObject.BlobObjectBsonConfig());
        ConfigureMap(new Message.MessageBsonConfig());
        Options = new BackupsModuleOptions
        {
            Enabled = true,
            WorkingDirectory = Path.Combine(Root, "backup"),
            MongodumpPath = Environment.GetEnvironmentVariable("GEEX_BACKUP_MONGODUMP") ?? "mongodump"
        };
        Services = CreateServices();
    }

    public ServiceProvider CreateServices() => new ServiceCollection().AddLogging()
            .AddSingleton(Options)
            .AddSingleton(new GeexCoreModuleOptions { ConnectionString = Uri })
            .AddSingleton(new BlobStorageModuleOptions { FileSystemStoragePath = Path.Combine(Root, "blobs") })
            .AddScoped<IUnitOfWork>(sp => new GeexDbContext(sp))
            .BuildServiceProvider();

    private static void ConfigureMap(object config)
    {
        var contract = config.GetType().GetInterface("Geex.IBsonConfig")!;
        contract.GetMethod("Map")!.Invoke(config, null);
    }

    public async Task DisposeAsync()
    {
        var errors = new List<Exception>();
        async Task Cleanup(Func<Task> action)
        {
            try { await action(); }
            catch (Exception error) { errors.Add(error); }
        }
        if (Services != null) await Cleanup(async () => await Services.DisposeAsync());
        if (_externalMongo) await Cleanup(async () =>
        {
            var database = new MongoClient(Uri).GetDatabase(DatabaseName);
            var rows = await database.GetCollection<BsonDocument>("Backup").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            var evidence = Environment.GetEnvironmentVariable("GEEX_BACKUP_TEST_EVIDENCE");
            if (!string.IsNullOrWhiteSpace(evidence))
            {
                var cases = Path.Combine(evidence, "cases");
                Directory.CreateDirectory(cases);
                await File.WriteAllTextAsync(Path.Combine(cases, "native-backup-observations.json"), JsonSerializer.Serialize(new
                {
                    testCase = "NativeBackupPersistedResults", recordedAt = DateTimeOffset.UtcNow,
                    observations = rows.Select(row => new
                    {
                        id = row["_id"].ToString(), status = row.GetValue("Status", BsonNull.Value).ToString(),
                        source = row.GetValue("Source", BsonNull.Value).ToString(),
                        hasFile = !row.GetValue("BlobObjectId", BsonNull.Value).IsBsonNull,
                        notificationSaved = row.GetValue("ResultNotificationSaved", false).ToBoolean()
                    }).ToArray()
                }));
            }
        });
        if (_externalMongo)
            foreach (var name in _additionalDatabases.Append(DatabaseName))
                await Cleanup(() => new MongoClient(Uri).DropDatabaseAsync(name));
        if (_mongo != null)
        {
            await Cleanup(async () =>
            {
                if (!_mongo.HasExited)
                {
                    _mongo.Kill(entireProcessTree: true);
                    await _mongo.WaitForExitAsync();
                }
            });
            _mongo.Dispose();
        }
        // Only this fixture's freshly created directory is removed.
        await Cleanup(async () =>
        {
            for (var attempt = 0; Directory.Exists(Root); attempt++)
            {
                try { Directory.Delete(Root, recursive: true); }
                catch (IOException) when (attempt < 20) { await Task.Delay(100); }
            }
        });
        if (errors.Count != 0) throw new AggregateException("Backup fixture cleanup did not complete.", errors);
    }
}

[CollectionDefinition("Backups", DisableParallelization = true)]
public class BackupCollection : ICollectionFixture<BackupFixture> { }

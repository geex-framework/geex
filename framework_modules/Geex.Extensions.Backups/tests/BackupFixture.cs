using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Geex.Extensions.BlobStorage;
using Geex.Extensions.BlobStorage.Core.Entities;
using Geex.Extensions.Backups.Core.Entities;
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
    public string Root { get; } = Path.Combine(Environment.GetEnvironmentVariable("GEEX_BACKUP_TEST_EVIDENCE") ?? Path.Combine(AppContext.BaseDirectory, ".test-evidence", "database-backup"), "geex-backup-tests-" + Guid.NewGuid().ToString("N"));
    public string DatabaseName { get; } = "geex_backup_test_" + Guid.NewGuid().ToString("N");
    public string Uri { get; private set; } = "";
    public ServiceProvider Services { get; private set; } = null!;
    public BackupsModuleOptions Options { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Path.Combine(Root, "mongo"));
        Directory.CreateDirectory(Path.Combine(Root, "blobs"));
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
        Services = new ServiceCollection().AddLogging()
            .AddSingleton(Options)
            .AddSingleton(new GeexCoreModuleOptions { ConnectionString = Uri })
            .AddSingleton(new BlobStorageModuleOptions { FileSystemStoragePath = Path.Combine(Root, "blobs") })
            .AddScoped<IUnitOfWork>(sp => new GeexDbContext(sp))
            .BuildServiceProvider();
    }

    private static void ConfigureMap(object config)
    {
        var contract = config.GetType().GetInterface("Geex.IBsonConfig")!;
        contract.GetMethod("Map")!.Invoke(config, null);
    }

    public async Task DisposeAsync()
    {
        if (Services != null) await Services.DisposeAsync();
        if (_mongo != null)
        {
            if (!_mongo.HasExited)
            {
                _mongo.Kill(entireProcessTree: true);
                await _mongo.WaitForExitAsync();
            }
            _mongo.Dispose();
        }
        // Only this fixture's freshly created directory is removed.
        for (var attempt = 0; Directory.Exists(Root); attempt++)
        {
            try { Directory.Delete(Root, recursive: true); }
            catch (IOException) when (attempt < 20) { await Task.Delay(100); }
        }
    }
}

[CollectionDefinition("Backups", DisableParallelization = true)]
public class BackupCollection : ICollectionFixture<BackupFixture> { }

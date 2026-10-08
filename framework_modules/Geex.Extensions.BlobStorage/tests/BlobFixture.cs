using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Geex.Extensions.BlobStorage.Core.Entities;
using Geex.Storage;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using MongoDB.Entities;
using Xunit;

namespace Geex.Extensions.BlobStorage.Tests;

public sealed class BlobFixture : IAsyncLifetime
{
    private Process? _mongo;
    public string Root { get; } = Path.Combine(Environment.GetEnvironmentVariable("GEEX_BLOB_TEST_EVIDENCE") ??
        Path.Combine(AppContext.BaseDirectory, ".test-evidence"), "blob-" + Guid.NewGuid().ToString("N"));
    public string DatabaseName { get; } = "geex_blob_test_" + Guid.NewGuid().ToString("N");
    public ServiceProvider Services { get; private set; } = null!;
    public string BlobDirectory => Path.Combine(Root, "blobs");

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.Combine(Root, "mongo"));
        Directory.CreateDirectory(BlobDirectory);
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var info = new ProcessStartInfo(Environment.GetEnvironmentVariable("GEEX_BLOB_MONGOD") ?? "mongod")
        { UseShellExecute = false, CreateNoWindow = true };
        foreach (var arg in new[] { "--bind_ip", "127.0.0.1", "--port", port.ToString(), "--dbpath",
                     Path.Combine(Root, "mongo"), "--logpath", Path.Combine(Root, "mongo.log") })
            info.ArgumentList.Add(arg);
        _mongo = Process.Start(info) ?? throw new InvalidOperationException("Could not start isolated mongod.");
        var uri = $"mongodb://127.0.0.1:{port}/{DatabaseName}";
        var settings = MongoClientSettings.FromConnectionString(uri);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
        settings.MaxConnectionPoolSize = 16;
        settings.LinqProvider = global::MongoDB.Driver.Linq.LinqProvider.V2;
        var commandLog = new object();
        settings.ClusterConfigurator = cluster => cluster.Subscribe<CommandStartedEvent>(command =>
        {
            if (command.CommandName is not ("aggregate" or "find")) return;
            lock (commandLog)
                File.AppendAllText(Path.Combine(Root, "queries.log"), command.Command.ToString() + Environment.NewLine);
        });
        await DB.InitAsync(DatabaseName, settings);
        DB.ChangeDefaultDatabase(DatabaseName);
        RuntimeHelpers.RunClassConstructor(typeof(GeexCoreModule).TypeHandle);
        ConfigureMap(new BlobObject.BlobObjectBsonConfig());
        ConfigureMap(new DbFile.DbFileEntityConfig());
        Services = new ServiceCollection().AddLogging()
            .AddSingleton(new GeexCoreModuleOptions { ConnectionString = uri, Host = "http://localhost" })
            .AddSingleton(new BlobStorageModuleOptions { FileSystemStoragePath = BlobDirectory })
            .AddScoped<IUnitOfWork>(sp => new GeexDbContext(sp)).BuildServiceProvider();
    }

    private static void ConfigureMap(object config) =>
        config.GetType().GetInterface("Geex.IBsonConfig")!.GetMethod("Map")!.Invoke(config, null);

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
    }
}

[CollectionDefinition("BlobStorage", DisableParallelization = true)]
public class BlobCollection : ICollectionFixture<BlobFixture> { }

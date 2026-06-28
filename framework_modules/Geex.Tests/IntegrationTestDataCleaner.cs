using Microsoft.Extensions.Configuration;

using MongoDB.Driver;

using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Configuration;

namespace Geex.Tests;

internal static class IntegrationTestDataCleaner
{
    public static async Task CleanAsync()
    {
        var configuration = BuildConfiguration();
        await CleanMongoDbAsync(configuration);
        await CleanRedisAsync(configuration);
        CleanBlobStorageFiles(configuration);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Test.json", optional: false)
            .Build();
    }

    private static async Task CleanMongoDbAsync(IConfiguration configuration)
    {
        var connectionString = configuration["GeexCoreModuleOptions:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var mongoUrl = MongoUrl.Create(connectionString);
        var client = new MongoClient(mongoUrl);
        await client.DropDatabaseAsync(mongoUrl.DatabaseName!);
    }

    private static async Task CleanRedisAsync(IConfiguration configuration)
    {
        var redisSection = configuration.GetSection("GeexCoreModuleOptions:Redis");
        var redisConfig = redisSection.Get<RedisConfiguration>();
        if (redisConfig?.Hosts is not { Length: > 0 })
        {
            return;
        }

        var options = new ConfigurationOptions
        {
            AllowAdmin = redisConfig.AllowAdmin,
            Password = redisConfig.Password,
            Ssl = redisConfig.Ssl,
            DefaultDatabase = redisConfig.Database,
            ConnectTimeout = redisConfig.ConnectTimeout,
            ConnectRetry = redisConfig.ConnectRetry ?? 2,
        };

        foreach (var host in redisConfig.Hosts)
        {
            options.EndPoints.Add(host.Host, host.Port);
        }

        using var connection = await ConnectionMultiplexer.ConnectAsync(options);
        foreach (var endpoint in connection.GetEndPoints())
        {
            var server = connection.GetServer(endpoint);
            if (server.IsReplica)
            {
                continue;
            }

            await server.FlushDatabaseAsync(redisConfig.Database);
        }
    }

    private static void CleanBlobStorageFiles(IConfiguration configuration)
    {
        var configuredPath = configuration["BlobStorageModuleOptions:FileSystemStoragePath"];
        var blobStoragePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(GeexConstants.AppDataPath, "BlobStorageFiles")
            : configuredPath;

        if (!Path.IsPathRooted(blobStoragePath))
        {
            blobStoragePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, blobStoragePath));
        }

        if (Directory.Exists(blobStoragePath))
        {
            Directory.Delete(blobStoragePath, recursive: true);
        }
    }
}

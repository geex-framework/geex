using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Geex.Extensions.Backups.Core;
using Geex.Extensions.Backups.Core.Entities;
using Geex.Extensions.Backups.Core.Jobs;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Entities;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using Xunit;

namespace Geex.Extensions.Backups.Tests;

[Collection("Backups")]
public class BackupLockTests(BackupFixture fixture)
{
    private IDatabase Redis => fixture.Services.GetRequiredService<IRedisDatabase>().Database;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ForceCancelsPreviousExecutorsAndOnlyReleasesItsOwnRegistration(bool olderFinishesFirst)
    {
        await using var first = await BackupLock.AcquireAsync(fixture.Services, CancellationToken.None);
        Assert.NotNull(first);
        await using var second = await BackupLock.AcquireAsync(fixture.Services, CancellationToken.None, force: true);
        Assert.NotNull(second);
        await using var third = await BackupLock.AcquireAsync(fixture.Services, CancellationToken.None, force: true);
        Assert.NotNull(third);
        await WaitUntil(() => first.Token.IsCancellationRequested && second.Token.IsCancellationRequested);
        Assert.False(third.Token.IsCancellationRequested);
        Assert.Equal(3, await Redis.HashLengthAsync(BackupLock.Key));
        Assert.Null(await Redis.KeyTimeToLiveAsync(BackupLock.Key));
        await (olderFinishesFirst ? first : third).DisposeAsync();
        Assert.Null(await BackupLock.AcquireAsync(fixture.Services, CancellationToken.None));
        await second.DisposeAsync();
        Assert.Equal(1, await Redis.HashLengthAsync(BackupLock.Key));
        Assert.Null(await BackupLock.AcquireAsync(fixture.Services, CancellationToken.None));
        await (olderFinishesFirst ? third : first).DisposeAsync();
        Assert.False(await Redis.KeyExistsAsync(BackupLock.Key));
        await using var next = await BackupLock.AcquireAsync(fixture.Services, CancellationToken.None);
        Assert.NotNull(next);
    }

    [Fact]
    public async Task ForceDoesNotTreatAnUnresponsiveExecutorAsStopped()
    {
        var stale = Guid.NewGuid().ToString("N");
        await Redis.HashSetAsync(BackupLock.Key, stale, "unresponsive-test-process");
        try
        {
            Assert.Null(await BackupLock.AcquireAsync(fixture.Services, CancellationToken.None));
            await using (var forced = await BackupLock.AcquireAsync(fixture.Services, CancellationToken.None, force: true))
            {
                Assert.NotNull(forced);
                Assert.Equal(2, await Redis.HashLengthAsync(BackupLock.Key));
                Assert.Null(await Redis.KeyTimeToLiveAsync(BackupLock.Key));
            }
            Assert.True(await Redis.HashExistsAsync(BackupLock.Key, stale));
            Assert.Null(await BackupLock.AcquireAsync(fixture.Services, CancellationToken.None));
        }
        finally { await Redis.HashDeleteAsync(BackupLock.Key, stale); }
    }

    [Fact]
    public async Task RedisFailureRejectsBackupBeforeCreatingARecord()
    {
        var count = await DB.Collection<Backup>().CountDocumentsAsync(FilterDefinition<Backup>.Empty);
        Assert.False(await Redis.KeyExistsAsync(BackupLock.Key));
        await Redis.StringSetAsync(BackupLock.Key, "invalid-lock-type");
        var execution = new BackupsExecution(fixture.Services);
        try
        {
            await Assert.ThrowsAsync<RedisServerException>(() => execution.TryStartManualWithIdAsync());
            Assert.Equal(count, await DB.Collection<Backup>().CountDocumentsAsync(FilterDefinition<Backup>.Empty));
        }
        finally
        {
            await execution.StopAsync(CancellationToken.None);
            await Redis.KeyDeleteAsync(BackupLock.Key);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ForcedBackupCancelsThePreviousMongodumpAndCompletes(bool sameInstance)
    {
        var core = fixture.Services.GetRequiredService<GeexCoreModuleOptions>();
        var original = core.ConnectionString;
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        core.ConnectionString = $"mongodb://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/{fixture.DatabaseName}";
        var first = new BackupsExecution(fixture.Services);
        var second = sameInstance ? first : new BackupsExecution(fixture.Services);
        Process? child = null;
        try
        {
            var previousId = await first.TryStartManualWithIdAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotNull(previousId);
            child = await FindMongodump(previousId);
            core.ConnectionString = original;
            var nextId = await second.TryStartManualWithIdAsync(force: true).WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotNull(nextId);
            await WaitUntil(async () => (await DB.Collection<Backup>().Find(x => x.Id == nextId).SingleAsync()).Status == BackupStatus.Succeeded);
            await first.StopAsync(CancellationToken.None);
            await second.StopAsync(CancellationToken.None);
            Assert.True(child.HasExited);
            var previous = await DB.Collection<Backup>().Find(x => x.Id == previousId).SingleAsync();
            Assert.Equal(BackupStatus.Cancelled, previous.Status);
            Assert.Null(previous.BlobObjectId);
            Assert.False(Directory.Exists(Path.Combine(fixture.Options.WorkingDirectory, previousId)));
            Assert.False(await Redis.KeyExistsAsync(BackupLock.Key));
        }
        finally
        {
            core.ConnectionString = original;
            await first.StopAsync(CancellationToken.None);
            await second.StopAsync(CancellationToken.None);
            child?.Dispose();
        }
    }

    [Fact]
    public async Task IndependentProcessesAndDatabasesShareTheLockAndForceCancellation()
    {
        var root = Path.Combine(fixture.Root, "workers-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var firstFile = Path.Combine(root, "first.json");
        using var first = StartWorker(firstFile, fixture.Uri, "normal", ((IPEndPoint)listener.LocalEndpoint).Port);
        var firstOutput = first.StandardOutput.ReadToEndAsync();
        var firstError = first.StandardError.ReadToEndAsync();
        Process? child = null;
        try
        {
            await WaitUntil(() => File.Exists(firstFile) || first.HasExited);
            Assert.True(File.Exists(firstFile), first.HasExited ? await firstError : "Worker did not start.");
            var firstId = JsonDocument.Parse(await File.ReadAllTextAsync(firstFile)).RootElement.GetProperty("id").GetString()!;
            child = await FindMongodump(firstId);
            var otherUri = new MongoUrlBuilder(fixture.Uri) { DatabaseName = fixture.AllocateDatabaseName("lock_other") }.ToString();
            var busy = await RunWorker(Path.Combine(root, "busy.json"), otherUri, "normal");
            Assert.Equal(JsonValueKind.Null, busy.GetProperty("id").ValueKind);
            var forced = await RunWorker(Path.Combine(root, "forced.json"), fixture.Uri, "force");
            Assert.Equal("Succeeded", forced.GetProperty("status").GetString());
            await first.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
            Assert.True(first.ExitCode == 0, await firstOutput + await firstError);
            Assert.True(child.HasExited);
            var previous = JsonDocument.Parse(await File.ReadAllTextAsync(firstFile + ".done")).RootElement;
            Assert.Equal("Cancelled", previous.GetProperty("status").GetString());
            Assert.False(await Redis.KeyExistsAsync(BackupLock.Key));
            var evidence = Environment.GetEnvironmentVariable("GEEX_BACKUP_TEST_EVIDENCE");
            if (!string.IsNullOrWhiteSpace(evidence))
            {
                Directory.CreateDirectory(Path.Combine(evidence, "cases"));
                await File.WriteAllTextAsync(Path.Combine(evidence, "cases", "global-backup-processes.json"), JsonSerializer.Serialize(new
                {
                    testCase = nameof(IndependentProcessesAndDatabasesShareTheLockAndForceCancellation),
                    firstProcess = first.Id, previousBackup = firstId, previousStatus = previous.GetProperty("status").GetString(),
                    forcedBackup = forced.GetProperty("id").GetString(), forcedStatus = forced.GetProperty("status").GetString(),
                    otherDatabaseRejected = true, previousMongodumpExited = child.HasExited, lockReleased = true
                }));
            }
        }
        finally
        {
            if (!first.HasExited) { first.Kill(true); await first.WaitForExitAsync(); }
            child?.Dispose();
        }
    }

    private Process StartWorker(string result, string uri, string mode, int blockedExportPort = 0)
    {
        var info = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { typeof(RedisWorker).Assembly.Location, "backup", fixture.RedisPort.ToString(), result, uri, mode, blockedExportPort.ToString() })
            info.ArgumentList.Add(argument);
        return Process.Start(info)!;
    }

    private async Task<JsonElement> RunWorker(string result, string uri, string mode)
    {
        using var process = StartWorker(result, uri, mode);
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
            Assert.True(process.ExitCode == 0, await output + await error);
            return JsonDocument.Parse(await File.ReadAllTextAsync(result + ".done")).RootElement.Clone();
        }
        finally { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); } }
    }

    private static async Task<Process> FindMongodump(string backupId)
    {
        Process? found = null;
        await WaitUntil(() =>
        {
            foreach (var process in Process.GetProcessesByName("mongodump"))
            {
                try
                {
                    if (File.ReadAllText($"/proc/{process.Id}/cmdline").Contains("backup-" + backupId + ".archive.gz", StringComparison.Ordinal))
                    { found = process; return true; }
                }
                catch (IOException) { }
                process.Dispose();
            }
            return false;
        });
        return found!;
    }

    private static Task WaitUntil(Func<bool> condition) => WaitUntil(() => Task.FromResult(condition()));
    private static async Task WaitUntil(Func<Task<bool>> condition)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (!await condition()) await Task.Delay(20, deadline.Token);
    }
}

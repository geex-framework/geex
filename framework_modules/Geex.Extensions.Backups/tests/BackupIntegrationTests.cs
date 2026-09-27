using System.Diagnostics;
using Geex.Extensions.BlobStorage;
using Geex.Extensions.BlobStorage.Core.Entities;
using Geex.Extensions.BlobStorage.Requests;
using Geex.Extensions.Backups.Core;
using Geex.Extensions.Backups.Core.Entities;
using Geex.Extensions.Backups.Core.Jobs;
using Geex.Extensions.Backups.Gql;
using Geex.Extensions.BackgroundJob;
using Geex.Storage;
using Microsoft.Extensions.Hosting;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Entities;
using Xunit;

namespace Geex.Extensions.Backups.Tests;

[Collection("Backups")]
public class BackupIntegrationTests(BackupFixture fixture)
{
    private static long _slot;
    private static DateTimeOffset NextSlot() => DateTimeOffset.UtcNow.AddDays(Interlocked.Increment(ref _slot));

    [Fact]
    public async Task ConstructorSaveAndRestorePreserveDocumentsTypesAndIndexes()
    {
        Assert.Equal("Backup", DB.CollectionName<Backup>());
        Assert.Equal("Backup", BsonClassMap.LookupClassMap(typeof(Backup)).Discriminator);
        var collectionName = "data_" + Guid.NewGuid().ToString("N");
        var data = DB.DefaultDb.GetCollection<BsonDocument>(collectionName);
        var document = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Code", "PIMS-001" },
            { "Amount", new BsonDecimal128(12.75m) }, { "Time", DateTime.UtcNow }, { "Bytes", new BsonBinaryData(new byte[] { 1, 2, 3 }) } };
        await data.InsertOneAsync(document);
        await data.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("Code"), new CreateIndexOptions { Unique = true, Name = "unique_code" }));
        var unrelated = DB.DefaultDb.Client.GetDatabase("unrelated_" + Guid.NewGuid().ToString("N"));
        await unrelated.GetCollection<BsonDocument>("sentinel").InsertOneAsync(new BsonDocument("value", 1));
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backup = new Backup(work, NextSlot());
        await work.SaveChanges();
        var blobId = backup.BlobObjectId;
        Assert.Equal(BackupStatus.Succeeded, backup.Status);
        var blob = Assert.IsType<BlobObject>(backup.File);
        Assert.Equal(BlobStorageType.FileSystem, blob.StorageType);
        Assert.True(File.Exists(blob.GetFilePath()));
        Assert.True(blob.FileSize > 0);
        await work.SaveChanges();
        Assert.Equal(blobId, backup.BlobObjectId);
        Assert.False(Directory.Exists(Path.Combine(fixture.Options.WorkingDirectory, backup.Id)));

        var roundtrip = BsonSerializer.Deserialize<Backup>(backup.ToBson());
        Assert.Equal(BackupSource.Manual, roundtrip.Source);
        var legacy = backup.ToBsonDocument();
        legacy.Remove(nameof(Backup.Source));
        Assert.Equal(BackupSource.Unknown, BsonSerializer.Deserialize<Backup>(legacy).Source);
        using var loadedScope = fixture.Services.CreateScope();
        var loadedWork = loadedScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        loadedWork.Attach(roundtrip);
        await loadedWork.SaveChanges();
        Assert.Equal(blobId, roundtrip.BlobObjectId);

        var restoredName = "restored_" + Guid.NewGuid().ToString("N");
        var info = new ProcessStartInfo(Environment.GetEnvironmentVariable("GEEX_BACKUP_MONGORESTORE") ?? "mongorestore")
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
        foreach (var arg in new[] { "--uri", fixture.Uri, "--archive=" + blob.GetFilePath(), "--gzip",
                     "--nsFrom=" + fixture.DatabaseName + ".*", "--nsTo=" + restoredName + ".*" })
            info.ArgumentList.Add(arg);
        using var restore = Process.Start(info)!;
        var error = restore.StandardError.ReadToEndAsync();
        var output = restore.StandardOutput.ReadToEndAsync();
        await restore.WaitForExitAsync();
        Assert.True(restore.ExitCode == 0, await error);
        await output;
        var restored = DB.DefaultDb.Client.GetDatabase(restoredName).GetCollection<BsonDocument>(collectionName);
        Assert.Equal(document, await restored.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync());
        Assert.Contains((await restored.Indexes.ListAsync()).ToList(), x => x["name"] == "unique_code" && x["unique"].AsBoolean);
        Assert.DoesNotContain("sentinel", (await DB.DefaultDb.Client.GetDatabase(restoredName).ListCollectionNamesAsync()).ToList());
        await backup.ExpireAsync();
        Assert.False(File.Exists(blob.GetFilePath()));
        Assert.Equal(BackupStatus.Expired, backup.Status);
    }

    [Fact]
    public async Task AllSaveCallbacksAreAwaitedInOrderAndErrorsPropagate()
    {
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var events = new List<int>();
        work.PreSaveChanges += async () => { await Task.Delay(40); events.Add(1); };
        work.PreSaveChanges += () => { Assert.Equal(new[] { 1 }, events); events.Add(2); return Task.CompletedTask; };
        work.PostSaveChanges += async () => { await Task.Delay(40); events.Add(3); };
        work.PostSaveChanges += () => { Assert.Equal(new[] { 1, 2, 3 }, events); events.Add(4); return Task.CompletedTask; };
        await work.SaveChanges();
        Assert.Equal(new[] { 1, 2, 3, 4 }, events);
        using var failureScope = fixture.Services.CreateScope();
        var failureWork = failureScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var afterRan = false;
        failureWork.PreSaveChanges += async () => { await Task.Yield(); throw new IOException("expected"); };
        failureWork.PreSaveChanges += () => Task.CompletedTask;
        failureWork.PostSaveChanges += () => { afterRan = true; return Task.CompletedTask; };
        await Assert.ThrowsAsync<IOException>(() => failureWork.SaveChanges());
        Assert.False(afterRan);

        using var postFailureScope = fixture.Services.CreateScope();
        var postFailureWork = postFailureScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        postFailureWork.PostSaveChanges += async () => { await Task.Yield(); throw new IOException("expected post-save failure"); };
        postFailureWork.PostSaveChanges += () => { afterRan = true; return Task.CompletedTask; };
        await Assert.ThrowsAsync<IOException>(() => postFailureWork.SaveChanges());
        Assert.False(afterRan);
    }

    [Fact]
    public async Task ExpirationFailurePreservesAssociationForRetry()
    {
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backup = new Backup(work, NextSlot());
        await work.SaveChanges();
        var blob = Assert.IsType<BlobObject>(backup.File);
        var filePath = blob.GetFilePath();
        var blockedPath = Path.Combine(Path.GetDirectoryName(filePath)!, blob.Id + ".upload");
        Directory.CreateDirectory(blockedPath);
        try
        {
            await Assert.ThrowsAnyAsync<Exception>(() => backup.ExpireAsync());
            var stored = await DB.Collection<Backup>().Find(x => x.Id == backup.Id).SingleAsync();
            Assert.Equal(BackupStatus.Succeeded, stored.Status);
            Assert.Equal(blob.Id, stored.BlobObjectId);
            Assert.StartsWith("Archive cleanup:", stored.ErrorMessage);
            Assert.Equal(stored.ErrorMessage, backup.ErrorMessage);
            Assert.True(File.Exists(filePath));
        }
        finally { Directory.Delete(blockedPath); }
        await backup.ExpireAsync();
        await work.SaveChanges();
        Assert.Equal(BackupStatus.Expired, backup.Status);
        Assert.Null(backup.BlobObjectId);
        Assert.Null(backup.ErrorMessage);
        var completed = await DB.Collection<Backup>().Find(x => x.Id == backup.Id).SingleAsync();
        Assert.Equal(BackupStatus.Expired, completed.Status);
        Assert.Null(completed.ErrorMessage);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task FinalCommitFailureIsCompensatedAndKeepsExistingBackups()
    {
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var before = await DB.Collection<Backup>().CountDocumentsAsync(x => x.Status == BackupStatus.Succeeded);
        var backup = new Backup(work, NextSlot());
        work.PreSaveChanges += () => throw new IOException("simulated commit failure");
        await Assert.ThrowsAsync<IOException>(() => work.SaveChanges());
        var path = backup.File!.GetFilePath();
        await backup.CompensateAsync(BackupStatus.Failed, "Test persistence failure.");
        var stored = await DB.Collection<Backup>().Find(x => x.Id == backup.Id).SingleAsync();
        Assert.Equal(BackupStatus.Failed, stored.Status);
        Assert.Null(stored.BlobObjectId);
        Assert.False(File.Exists(path));
        Assert.Equal(before, await DB.Collection<Backup>().CountDocumentsAsync(x => x.Status == BackupStatus.Succeeded));
    }

    [Fact]
    public async Task ExportFailureLeavesRecordAndNoTemporaryFiles()
    {
        var previous = fixture.Options.MongodumpPath;
        fixture.Options.MongodumpPath = Path.Combine(fixture.Root, "missing-mongodump");
        try
        {
            using var scope = fixture.Services.CreateScope();
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var backup = new Backup(work, NextSlot());
            await Assert.ThrowsAnyAsync<Exception>(() => work.SaveChanges());
            var stored = await DB.Collection<Backup>().Find(x => x.Id == backup.Id).SingleAsync();
            Assert.Equal(BackupStatus.Failed, stored.Status);
            Assert.Null(stored.BlobObjectId);
            Assert.False(Directory.Exists(Path.Combine(fixture.Options.WorkingDirectory, backup.Id)));
        }
        finally { fixture.Options.MongodumpPath = previous; }
    }

    [Fact]
    public async Task ToolFailurePersistsDiagnosticWithoutCredentials()
    {
        var core = fixture.Services.GetRequiredService<GeexCoreModuleOptions>();
        var originalUri = core.ConnectionString;
        core.ConnectionString = originalUri.Replace("mongodb://", "mongodb://diagnosticUser:privatePassword@");
        try
        {
            using var scope = fixture.Services.CreateScope();
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var backup = new Backup(work, NextSlot());
            await Assert.ThrowsAsync<InvalidOperationException>(() => work.SaveChanges());
            var stored = await DB.Collection<Backup>().Find(x => x.Id == backup.Id).SingleAsync();
            Assert.Equal(BackupStatus.Failed, stored.Status);
            Assert.StartsWith("Archive export: mongodump failed with exit code", stored.ErrorMessage);
            Assert.Contains("auth", stored.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("privatePassword", stored.ErrorMessage);
            Assert.DoesNotContain("diagnosticUser", stored.ErrorMessage);
            Assert.DoesNotContain("mongodb://", stored.ErrorMessage);
        }
        finally { core.ConnectionString = originalUri; }
    }

    [Fact]
    public async Task LeaseDeduplicatesSlotsAndCancelsWhenOwnershipChanges()
    {
        var slot = NextSlot();
        await using var first = await BackupLease.AcquireAsync(DB.DefaultDb, slot, CancellationToken.None);
        Assert.NotNull(first);
        Assert.Null(await BackupLease.AcquireAsync(DB.DefaultDb, slot, CancellationToken.None));
        Assert.Null(await BackupLease.AcquireAsync(DB.DefaultDb, slot.AddDays(1), CancellationToken.None));
        var leases = DB.DefaultDb.GetCollection<BsonDocument>("_geex_database_backup_leases");
        await leases.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", fixture.DatabaseName),
            Builders<BsonDocument>.Update.Set("Owner", "replacement"));
        await Assert.ThrowsAsync<OperationCanceledException>(() => first.EnsureOwnedAsync());
        Assert.True(first.Token.IsCancellationRequested);
        await leases.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", fixture.DatabaseName),
            Builders<BsonDocument>.Update.Set("ExpiresAt", DateTime.UnixEpoch));
        Assert.Null(await BackupLease.AcquireAsync(DB.DefaultDb, slot, CancellationToken.None));
        await using var next = await BackupLease.AcquireAsync(DB.DefaultDb, slot.AddDays(1), CancellationToken.None);
        Assert.NotNull(next);
    }

    [Fact]
    public async Task AutomaticRetentionKeepsSevenAndPreservesManualLegacyAndUnrelatedFiles()
    {
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var unrelated = new BlobObject(new CreateBlobObjectRequest
        {
            File = new StreamFile("unrelated.txt", () => new MemoryStream(new byte[] { 9, 8, 7 })),
            StorageType = BlobStorageType.FileSystem
        }, work);
        await work.SaveChanges();
        var protectedFiles = new List<(string Id, string BlobId, string Path)>();
        foreach (var source in new[] { BackupSource.Manual, BackupSource.Unknown })
        {
            using var backupScope = fixture.Services.CreateScope();
            var backupWork = backupScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var backup = new Backup(backupWork, NextSlot(), source: source);
            await backupWork.SaveChanges();
            var blob = Assert.IsType<BlobObject>(backup.File);
            protectedFiles.Add((backup.Id, blob.Id, blob.GetFilePath()));
            if (source == BackupSource.Unknown)
                await DB.Collection<Backup>().UpdateOneAsync(x => x.Id == backup.Id,
                    Builders<Backup>.Update.Unset(x => x.Source));
        }
        var automaticFiles = new List<(string Id, string BlobId, string Path)>();
        for (var i = 0; i < 9; i++)
        {
            using var backupScope = fixture.Services.CreateScope();
            var backupWork = backupScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var backup = new Backup(backupWork, NextSlot(), source: BackupSource.Automatic);
            await backupWork.SaveChanges();
            var blob = Assert.IsType<BlobObject>(backup.File);
            automaticFiles.Add((backup.Id, blob.Id, blob.GetFilePath()));
        }
        var execution = new BackupsExecution(fixture.Services);
        try
        {
            var manualId = await execution.TryStartManualWithIdAsync();
            Assert.NotNull(manualId);
            Assert.Equal(BackupStatus.Succeeded, (await WaitForCompletionAsync(manualId)).Status);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (true)
            {
                await using var lease = await BackupLease.AcquireAsync(DB.DefaultDb, DateTimeOffset.UtcNow,
                    deadline.Token, deduplicateSlot: false);
                if (lease != null) break;
                await Task.Delay(20, deadline.Token);
            }
            foreach (var file in automaticFiles)
                Assert.Equal(BackupStatus.Succeeded, (await DB.Collection<Backup>().Find(x => x.Id == file.Id).SingleAsync()).Status);
            using var manualScope = fixture.Services.CreateScope();
            var manualWork = manualScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var manual = manualWork.Query<Backup>().Single(x => x.Id == manualId);
            var manualBlob = Assert.IsType<BlobObject>(manual.File);
            protectedFiles.Add((manual.Id, manualBlob.Id, manualBlob.GetFilePath()));

            await execution.RunScheduledAsync(NextSlot(), CancellationToken.None);
        }
        finally { await execution.StopAsync(CancellationToken.None); }
        var remaining = await DB.Collection<Backup>().Find(x => x.Status == BackupStatus.Succeeded && x.Source == BackupSource.Automatic)
            .SortByDescending(x => x.ScheduledAt).ToListAsync();
        Assert.Equal(7, remaining.Count);
        Assert.Equal(automaticFiles.TakeLast(6).Reverse().Select(x => x.Id), remaining.Skip(1).Select(x => x.Id));
        foreach (var file in automaticFiles.Take(3))
        {
            var expired = await DB.Collection<Backup>().Find(x => x.Id == file.Id).SingleAsync();
            Assert.Equal(BackupStatus.Expired, expired.Status);
            Assert.Null(expired.BlobObjectId);
            Assert.False(File.Exists(file.Path));
            Assert.Null(await DB.Collection<BlobObject>().Find(x => x.Id == file.BlobId).FirstOrDefaultAsync());
        }
        foreach (var file in protectedFiles)
        {
            var stored = await DB.Collection<Backup>().Find(x => x.Id == file.Id).SingleAsync();
            Assert.Equal(BackupStatus.Succeeded, stored.Status);
            Assert.Equal(file.BlobId, stored.BlobObjectId);
            Assert.True(File.Exists(file.Path));
            Assert.NotNull(await DB.Collection<BlobObject>().Find(x => x.Id == file.BlobId).FirstOrDefaultAsync());
        }
        Assert.True(File.Exists(unrelated.GetFilePath()));

        foreach (var file in protectedFiles)
        {
            using var expireScope = fixture.Services.CreateScope();
            var expireWork = expireScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            Assert.True(await new BackupMutation(expireWork, execution).ExpireBackup(file.Id, CancellationToken.None));
            Assert.False(File.Exists(file.Path));
        }
    }

    [Fact]
    public async Task InterruptedBackupRecoversFileAndTemporaryDirectory()
    {
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backup = new Backup(work, NextSlot());
        work.PreSaveChanges += () => throw new IOException("interrupted before final commit");
        await Assert.ThrowsAsync<IOException>(() => work.SaveChanges());
        var blob = Assert.IsType<BlobObject>(backup.File);
        var directory = Path.Combine(fixture.Options.WorkingDirectory, backup.Id);
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "partial"), "incomplete");
        await Backup.RecoverAsync(fixture.Services, fixture.DatabaseName, CancellationToken.None);
        var recovered = await DB.Collection<Backup>().Find(x => x.Id == backup.Id).SingleAsync();
        Assert.Equal(BackupStatus.Failed, recovered.Status);
        Assert.Null(recovered.BlobObjectId);
        Assert.False(File.Exists(blob.GetFilePath()));
        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public async Task BlobWriteCancellationDisposesStreamAndRemovesPartialFile()
    {
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        using var cancel = new CancellationTokenSource();
        var stream = new BlockingStream();
        var blob = new BlobObject(new CreateBlobObjectRequest
        {
            File = new StreamFile("cancelled.archive.gz", () => stream),
            StorageType = BlobStorageType.FileSystem
        }, work, cancel.Token);
        await stream.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => work.SaveChanges());
        Assert.True(stream.Disposed);
        Assert.False(File.Exists(Path.Combine(fixture.Root, "blobs", blob.Id + ".upload")));
        await blob.DeleteAsync();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ManualBackupWorksRegardlessOfAutomaticSwitches(bool automaticEnabled, bool jobsDisabled)
    {
        var options = new BackupsModuleOptions
        {
            Enabled = automaticEnabled,
            WorkingDirectory = fixture.Options.WorkingDirectory,
            MongodumpPath = fixture.Options.MongodumpPath,
            RetentionCount = automaticEnabled && !jobsDisabled ? 7 : 0
        };
        var jobs = new BackgroundJobModuleOptions { Disabled = jobsDisabled };
        if (automaticEnabled && !jobsDisabled) jobs.JobConfigs[nameof(BackupsJob)] = "0 0 3 * * *";
        var services = new ServiceCollection().AddLogging().AddSingleton(options).AddSingleton(jobs)
            .AddSingleton(fixture.Services.GetRequiredService<GeexCoreModuleOptions>())
            .AddSingleton(fixture.Services.GetRequiredService<BlobStorageModuleOptions>())
            .AddScoped<IUnitOfWork>(sp => new GeexDbContext(sp));
        BackupsModule.RegisterJobs(services, options);
        using var provider = services.BuildServiceProvider();
        var execution = provider.GetRequiredService<BackupsExecution>();
        try
        {
            await execution.StartAsync(CancellationToken.None);
            using var scope = provider.CreateScope();
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var mutation = new BackupMutation(work, execution);
            var id = await mutation.StartBackupTracked();
            Assert.NotNull(id);
            var stored = await WaitForCompletionAsync(id);
            Assert.Equal(BackupStatus.Succeeded, stored.Status);
            Assert.Equal(BackupSource.Manual, stored.Source);
            Assert.NotNull(stored.BlobObjectId);
            Assert.Equal(automaticEnabled && !jobsDisabled, new BackupQuery(work, options, jobs).BackupsEnabled());
            Assert.Equal(automaticEnabled && !jobsDisabled ? 1 : 0, provider.GetServices<IHostedService>().OfType<BackupsJob>().Count());
        }
        finally { await execution.StopAsync(CancellationToken.None); }
    }

    private static async Task<Backup> WaitForCompletionAsync(string id)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (true)
        {
            var stored = await DB.Collection<Backup>().Find(x => x.Id == id).SingleAsync(deadline.Token);
            if (stored.Status != BackupStatus.Running) return stored;
            await Task.Delay(20, deadline.Token);
        }
    }

    [Fact]
    public async Task TimeoutTerminatesExportAndPersistsCancellation()
    {
        var core = fixture.Services.GetRequiredService<GeexCoreModuleOptions>();
        var originalUri = core.ConnectionString;
        var originalTimeout = fixture.Options.Timeout;
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        // A listening socket without a MongoDB handshake keeps mongodump busy until cancellation.
        core.ConnectionString = $"mongodb://127.0.0.1:{port}/{fixture.DatabaseName}";
        fixture.Options.Timeout = TimeSpan.FromMilliseconds(500);
        try
        {
            using var scope = fixture.Services.CreateScope();
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var elapsed = Stopwatch.StartNew();
            var backup = new Backup(work, NextSlot());
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => work.SaveChanges());
            Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(10));
            var stored = await DB.Collection<Backup>().Find(x => x.Id == backup.Id).SingleAsync();
            Assert.Equal(BackupStatus.Cancelled, stored.Status);
            Assert.False(Directory.Exists(Path.Combine(fixture.Options.WorkingDirectory, backup.Id)));
        }
        finally
        {
            core.ConnectionString = originalUri;
            fixture.Options.Timeout = originalTimeout;
        }
    }

    [Fact]
    public async Task OriginalBlobHandlerAndSharedFileDeletionRemainCompatible()
    {
        var bytes = new byte[] { 4, 7, 1, 5, 2, 9 };
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var handler = new BlobStorage.Core.Handlers.BlobObjectHandler(work);
        var first = await handler.Handle(new CreateBlobObjectRequest
        {
            File = new StreamFile("first-shared.txt", () => new MemoryStream(bytes)),
            StorageType = BlobStorageType.FileSystem
        }, CancellationToken.None);
        var second = await handler.Handle(new CreateBlobObjectRequest
        {
            File = new StreamFile("second-shared.txt", () => new MemoryStream(bytes)),
            StorageType = BlobStorageType.FileSystem
        }, CancellationToken.None);
        await work.SaveChanges();
        Assert.Equal(first.Md5, second.Md5);
        var path = second.GetFilePath();
        await ((BlobObject)first).DeleteAsync();
        Assert.True(File.Exists(path));
        await ((BlobObject)second).DeleteAsync();
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task ApplicationStopCancelsAndWaitsForActiveBackup()
    {
        var core = fixture.Services.GetRequiredService<GeexCoreModuleOptions>();
        var originalUri = core.ConnectionString;
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        core.ConnectionString = $"mongodb://127.0.0.1:{port}/{fixture.DatabaseName}";
        await DB.DefaultDb.GetCollection<BsonDocument>("_geex_database_backup_leases")
            .DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        var job = new BackupsExecution(fixture.Services);
        var running = job.RunScheduledAsync(NextSlot(), CancellationToken.None);
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            Backup? record;
            do
            {
                await Task.Delay(20, deadline.Token);
                record = await DB.Collection<Backup>().Find(x => x.Status == BackupStatus.Running)
                    .FirstOrDefaultAsync(deadline.Token);
            } while (record == null);
            await job.StopAsync(deadline.Token);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
            var stored = await DB.Collection<Backup>().Find(x => x.Id == record.Id).SingleAsync();
            Assert.Equal(BackupStatus.Cancelled, stored.Status);
            Assert.Null(stored.BlobObjectId);
            Assert.False(Directory.Exists(Path.Combine(fixture.Options.WorkingDirectory, stored.Id)));
        }
        finally
        {
            await job.StopAsync(CancellationToken.None);
            core.ConnectionString = originalUri;
        }
    }

    [Fact]
    public async Task ConcurrentLeaseClaimsHaveExactlyOneOwner()
    {
        var database = DB.DefaultDb.Client.GetDatabase("lease_test_" + Guid.NewGuid().ToString("N"));
        var slot = DateTimeOffset.UtcNow;
        var claims = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => BackupLease.AcquireAsync(database, slot, CancellationToken.None)));
        var owner = Assert.Single(claims.Where(x => x != null));
        await owner!.DisposeAsync();
        Assert.Null(await BackupLease.AcquireAsync(database, slot, CancellationToken.None));
    }

    [Fact]
    public async Task ManualClaimsShareLeaseWithoutConsumingScheduledSlots()
    {
        var database = DB.DefaultDb.Client.GetDatabase("manual_lease_" + Guid.NewGuid().ToString("N"));
        var slot = DateTimeOffset.UtcNow;
        await using (var manual = await BackupLease.AcquireAsync(database, slot.AddMinutes(1), CancellationToken.None, false))
        {
            Assert.NotNull(manual);
            Assert.Null(await BackupLease.AcquireAsync(database, slot, CancellationToken.None));
            Assert.Null(await BackupLease.AcquireAsync(database, slot.AddMinutes(2), CancellationToken.None, false));
        }
        await using var scheduled = await BackupLease.AcquireAsync(database, slot, CancellationToken.None);
        Assert.NotNull(scheduled);
    }

    [Fact]
    public async Task ManualJobAcknowledgesEarlyAndRejectsConcurrentClaims()
    {
        var core = fixture.Services.GetRequiredService<GeexCoreModuleOptions>();
        var originalUri = core.ConnectionString;
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        core.ConnectionString = $"mongodb://127.0.0.1:{((System.Net.IPEndPoint)listener.LocalEndpoint).Port}/{fixture.DatabaseName}";
        var job = new BackupsExecution(fixture.Services);
        try
        {
            var id = await job.TryStartManualWithIdAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.NotNull(id);
            Assert.NotNull(await DB.Collection<Backup>().Find(x => x.Id == id).FirstOrDefaultAsync());
            Assert.False(await job.TryStartManualAsync());
            var anotherInstance = new BackupsExecution(fixture.Services);
            Assert.False(await anotherInstance.TryStartManualAsync());
            await anotherInstance.StopAsync(CancellationToken.None);
        }
        finally
        {
            await job.StopAsync(CancellationToken.None);
            core.ConnectionString = originalUri;
        }
        Assert.False(await job.TryStartManualAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task PartialDeletionRequiresExplicitIdempotentRetry(int deletionStage)
    {
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backup = new Backup(work, NextSlot());
        await work.SaveChanges();
        var blob = Assert.IsType<BlobObject>(backup.File);
        var path = blob.GetFilePath();
        if (deletionStage >= 1) File.Delete(path);
        if (deletionStage >= 2) await blob.DeleteAsync();

        await Backup.RecoverAsync(fixture.Services, fixture.DatabaseName, CancellationToken.None);
        var unchanged = await DB.Collection<Backup>().Find(x => x.Id == backup.Id).SingleAsync();
        Assert.Equal(BackupStatus.Succeeded, unchanged.Status);
        Assert.Equal(blob.Id, unchanged.BlobObjectId);
        Assert.Equal(deletionStage == 0, File.Exists(path));
        Assert.Equal(deletionStage < 2, await DB.Collection<BlobObject>().Find(x => x.Id == blob.Id).AnyAsync());
        var execution = new BackupsExecution(fixture.Services);
        var mutation = new BackupMutation(work, execution);
        Assert.True(await mutation.ExpireBackup(backup.Id, CancellationToken.None));
        var stored = await DB.Collection<Backup>().Find(x => x.Id == backup.Id).SingleAsync();
        Assert.Equal(BackupStatus.Expired, stored.Status);
        Assert.Null(stored.BlobObjectId);
        Assert.Null(stored.ErrorMessage);
        Assert.False(File.Exists(path));
        Assert.Null(await DB.Collection<BlobObject>().Find(x => x.Id == blob.Id).FirstOrDefaultAsync());
        Assert.True(await mutation.ExpireBackup(backup.Id, CancellationToken.None));
        await backup.ExpireAsync();
        Assert.Equal(BackupStatus.Expired, backup.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CleanupFailureDoesNotBlockOrGetRetriedByNewBackup(bool automatic)
    {
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var previous = new Backup(work, NextSlot());
        await work.SaveChanges();
        var blob = Assert.IsType<BlobObject>(previous.File);
        var blockedPath = Path.Combine(Path.GetDirectoryName(blob.GetFilePath())!, blob.Id + ".upload");
        Directory.CreateDirectory(blockedPath);
        var job = new BackupsExecution(fixture.Services);
        string? newBackupId = null;
        try
        {
            await Assert.ThrowsAnyAsync<Exception>(() => previous.ExpireAsync());
            var error = previous.ErrorMessage;
            if (automatic)
            {
                var slot = NextSlot();
                await job.RunScheduledAsync(slot, CancellationToken.None);
                newBackupId = (await DB.Collection<Backup>().Find(x => x.ScheduledAt == slot).SingleAsync()).Id;
            }
            else
                newBackupId = await job.TryStartManualWithIdAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.NotNull(newBackupId);
            Assert.Equal(BackupStatus.Succeeded, (await WaitForCompletionAsync(newBackupId)).Status);
            Directory.Delete(blockedPath);
            await Backup.RecoverAsync(fixture.Services, fixture.DatabaseName, CancellationToken.None);
            var unchanged = await DB.Collection<Backup>().Find(x => x.Id == previous.Id).SingleAsync();
            Assert.Equal(BackupStatus.Succeeded, unchanged.Status);
            Assert.Equal(error, unchanged.ErrorMessage);
            Assert.Equal(blob.Id, unchanged.BlobObjectId);
            Assert.True(File.Exists(blob.GetFilePath()));
        }
        finally
        {
            await job.StopAsync(CancellationToken.None);
            if (Directory.Exists(blockedPath)) Directory.Delete(blockedPath);
            await previous.ExpireAsync();
            if (newBackupId != null)
            {
                using var cleanupScope = fixture.Services.CreateScope();
                var cleanupWork = cleanupScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await cleanupWork.Query<Backup>().Single(x => x.Id == newBackupId).ExpireAsync();
            }
        }
    }

    [Fact]
    public async Task AutomaticRetentionContinuesAfterCleanupFailureAndDoesNotRetryFailedCleanup()
    {
        var backups = new List<Backup>();
        var scopes = new List<IServiceScope>();
        string? blockedPath = null;
        try
        {
            for (var index = 0; index < 4; index++)
            {
                var scope = fixture.Services.CreateScope();
                scopes.Add(scope);
                var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var backup = new Backup(work, NextSlot(), source: BackupSource.Automatic);
                await work.SaveChanges();
                backups.Add(backup);
            }
            var blocked = backups[2];
            var blockedBlob = Assert.IsType<BlobObject>(blocked.File);
            blockedPath = Path.Combine(Path.GetDirectoryName(blockedBlob.GetFilePath())!, blockedBlob.Id + ".upload");
            Directory.CreateDirectory(blockedPath);
            await BackupsExecution.PruneAsync(fixture.Services, fixture.DatabaseName, 1, CancellationToken.None);
            var failed = await DB.Collection<Backup>().Find(x => x.Id == blocked.Id).SingleAsync();
            Assert.Equal(BackupStatus.Succeeded, failed.Status);
            Assert.StartsWith("Archive cleanup:", failed.ErrorMessage);
            foreach (var backup in backups.Take(2))
                Assert.Equal(BackupStatus.Expired, (await DB.Collection<Backup>().Find(x => x.Id == backup.Id).SingleAsync()).Status);
            Directory.Delete(blockedPath);
            await DB.Collection<Backup>().UpdateOneAsync(x => x.Id == blocked.Id,
                Builders<Backup>.Update.Set(x => x.ScheduledAt, NextSlot()));
            await BackupsExecution.PruneAsync(fixture.Services, fixture.DatabaseName, 1, CancellationToken.None);
            var unchanged = await DB.Collection<Backup>().Find(x => x.Id == blocked.Id).SingleAsync();
            Assert.Equal(failed.ErrorMessage, unchanged.ErrorMessage);
            Assert.Equal(BackupStatus.Succeeded, unchanged.Status);
            Assert.True(File.Exists(blockedBlob.GetFilePath()));
            Assert.Equal(BackupStatus.Succeeded, (await DB.Collection<Backup>().Find(x => x.Id == backups[3].Id).SingleAsync()).Status);
        }
        finally
        {
            if (blockedPath != null && Directory.Exists(blockedPath)) Directory.Delete(blockedPath);
            foreach (var backup in backups) await backup.ExpireAsync();
            foreach (var scope in scopes) scope.Dispose();
        }
    }

    [Fact]
    public async Task ExpirationRejectsBusyLeaseAndReadsCurrentState()
    {
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backup = new Backup(work, NextSlot());
        await work.SaveChanges();
        var blob = Assert.IsType<BlobObject>(backup.File);
        var mutation = new BackupMutation(work, new BackupsExecution(fixture.Services));
        await using (var lease = await BackupLease.AcquireAsync(DB.DefaultDb, DateTimeOffset.UtcNow, CancellationToken.None, false))
        {
            Assert.NotNull(lease);
            Assert.False(await mutation.ExpireBackup(backup.Id, CancellationToken.None));
            Assert.False(await mutation.ExpireBackup(ObjectId.GenerateNewId().ToString(), CancellationToken.None));
            Assert.True(File.Exists(blob.GetFilePath()));
        }
        await DB.Collection<Backup>().UpdateOneAsync(x => x.Id == backup.Id,
            Builders<Backup>.Update.Set(x => x.Status, BackupStatus.Running));
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => mutation.ExpireBackup(backup.Id, CancellationToken.None));
            Assert.True(File.Exists(blob.GetFilePath()));
        }
        finally
        {
            await DB.Collection<Backup>().UpdateOneAsync(x => x.Id == backup.Id,
                Builders<Backup>.Update.Set(x => x.Status, BackupStatus.Succeeded));
            Assert.True(await mutation.ExpireBackup(backup.Id, CancellationToken.None));
        }
        await backup.ExpireAsync();
        Assert.Equal(BackupStatus.Expired, backup.Status);
        Assert.Null(backup.BlobObjectId);
    }

    [Fact]
    public async Task ExpirationCancellationAndLostLeaseLeaveArchiveUntouched()
    {
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backup = new Backup(work, NextSlot());
        await work.SaveChanges();
        var blob = Assert.IsType<BlobObject>(backup.File);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => backup.ExpireAsync(cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            BackupsExecution.PruneAsync(fixture.Services, fixture.DatabaseName, 0, cancellation.Token));
        var leases = DB.DefaultDb.GetCollection<BsonDocument>("_geex_database_backup_leases");
        await using (var lease = await BackupLease.AcquireAsync(DB.DefaultDb, DateTimeOffset.UtcNow, CancellationToken.None, false))
        {
            Assert.NotNull(lease);
            await leases.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", fixture.DatabaseName),
                Builders<BsonDocument>.Update.Set("Owner", "expiration-test-replacement"));
            try
            {
                await Assert.ThrowsAsync<OperationCanceledException>(() => lease.EnsureOwnedAsync());
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => backup.ExpireAsync(lease.Token));
                var stored = await DB.Collection<Backup>().Find(x => x.Id == backup.Id).SingleAsync();
                Assert.Equal(BackupStatus.Succeeded, stored.Status);
                Assert.Null(stored.ErrorMessage);
                Assert.Equal(blob.Id, stored.BlobObjectId);
                Assert.True(File.Exists(blob.GetFilePath()));
            }
            finally
            {
                await leases.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", fixture.DatabaseName),
                    Builders<BsonDocument>.Update.Set("ExpiresAt", DateTime.UnixEpoch));
            }
        }
        await backup.ExpireAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CleanupRechecksLeaseAfterDeletionBeforeRecordingCompletion(bool automatic)
    {
        using var originalScope = fixture.Services.CreateScope();
        var originalWork = originalScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backup = new Backup(originalWork, NextSlot(), source: BackupSource.Automatic);
        await originalWork.SaveChanges();
        var blob = backup.File!;
        var path = blob.GetFilePath();
        var leases = DB.DefaultDb.GetCollection<BsonDocument>("_geex_database_backup_leases");
        var replaceOwner = 1;
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton(fixture.Options)
            .AddSingleton(fixture.Services.GetRequiredService<GeexCoreModuleOptions>())
            .AddSingleton(fixture.Services.GetRequiredService<BlobStorageModuleOptions>())
            .AddScoped<IUnitOfWork>(sp =>
            {
                var work = new GeexDbContext(sp);
                work.PostSaveChanges += async () =>
                {
                    if (Interlocked.Exchange(ref replaceOwner, 0) != 1) return;
                    Assert.False(File.Exists(path));
                    Assert.Null(await DB.Collection<BlobObject>().Find(x => x.Id == blob.Id).FirstOrDefaultAsync());
                    await leases.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", fixture.DatabaseName),
                        Builders<BsonDocument>.Update.Set("Owner", "cleanup-completion-test-replacement"));
                };
                return work;
            }).BuildServiceProvider();
        try
        {
            if (automatic)
            {
                await using var lease = await BackupLease.AcquireAsync(DB.DefaultDb, DateTimeOffset.UtcNow, CancellationToken.None, false);
                Assert.NotNull(lease);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    BackupsExecution.PruneAsync(services, fixture.DatabaseName, 0, lease.Token, lease.EnsureOwnedAsync));
                Assert.True(lease.Token.IsCancellationRequested);
            }
            else
            {
                using var scope = services.CreateScope();
                var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var mutation = new BackupMutation(work, new BackupsExecution(services));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mutation.ExpireBackup(backup.Id, CancellationToken.None));
            }
            Assert.Equal(0, replaceOwner);
            var stored = await DB.Collection<Backup>().Find(x => x.Id == backup.Id).SingleAsync();
            Assert.Equal(BackupStatus.Succeeded, stored.Status);
            Assert.Equal(blob.Id, stored.BlobObjectId);
            Assert.Null(stored.ErrorMessage);
            Assert.False(File.Exists(path));
            Assert.Null(await DB.Collection<BlobObject>().Find(x => x.Id == blob.Id).FirstOrDefaultAsync());
        }
        finally
        {
            Interlocked.Exchange(ref replaceOwner, 0);
            await leases.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", fixture.DatabaseName),
                Builders<BsonDocument>.Update.Set("ExpiresAt", DateTime.UnixEpoch));
            await backup.ExpireAsync();
        }
    }

    [Fact]
    public async Task CleanupPersistenceFailurePropagatesAndAllowsExplicitRetry()
    {
        var failSave = false;
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton(fixture.Options)
            .AddSingleton(fixture.Services.GetRequiredService<GeexCoreModuleOptions>())
            .AddSingleton(fixture.Services.GetRequiredService<BlobStorageModuleOptions>())
            .AddScoped<IUnitOfWork>(sp =>
            {
                var work = new GeexDbContext(sp);
                work.PreSaveChanges += () => failSave
                    ? Task.FromException(new InvalidOperationException("simulated cleanup persistence failure"))
                    : Task.CompletedTask;
                return work;
            }).BuildServiceProvider();
        using var scope = services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var backup = new Backup(work, NextSlot(), source: BackupSource.Automatic);
        await work.SaveChanges();
        var blob = backup.File!;
        failSave = true;
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                BackupsExecution.PruneAsync(services, fixture.DatabaseName, 0, CancellationToken.None));
            var stored = await DB.Collection<Backup>().Find(x => x.Id == backup.Id).SingleAsync();
            Assert.Equal(BackupStatus.Succeeded, stored.Status);
            Assert.Equal(blob.Id, stored.BlobObjectId);
            Assert.Contains("simulated cleanup persistence failure", stored.ErrorMessage);
            Assert.False(File.Exists(blob.GetFilePath()));
        }
        finally { failSave = false; }
        await backup.ExpireAsync();
        var completed = await DB.Collection<Backup>().Find(x => x.Id == backup.Id).SingleAsync();
        Assert.Equal(BackupStatus.Expired, completed.Status);
        Assert.Null(completed.BlobObjectId);
        Assert.Null(completed.ErrorMessage);
    }

    [Fact]
    public async Task FinalCleanupStateWriteFailureStopsRetentionAndAllowsExplicitRetry()
    {
        using var olderScope = fixture.Services.CreateScope();
        var olderWork = olderScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var older = new Backup(olderWork, NextSlot(), source: BackupSource.Automatic);
        await olderWork.SaveChanges();
        using var currentScope = fixture.Services.CreateScope();
        var currentWork = currentScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var current = new Backup(currentWork, NextSlot(), source: BackupSource.Automatic);
        await currentWork.SaveChanges();
        var blob = current.File!;
        var path = blob.GetFilePath();
        var olderPath = older.File!.GetFilePath();
        var admin = DB.DefaultDb.Client.GetDatabase("admin");
        var injectFailure = 1;
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton(fixture.Options)
            .AddSingleton(fixture.Services.GetRequiredService<GeexCoreModuleOptions>())
            .AddSingleton(fixture.Services.GetRequiredService<BlobStorageModuleOptions>())
            .AddScoped<IUnitOfWork>(sp =>
            {
                var work = new GeexDbContext(sp);
                work.PostSaveChanges += async () =>
                {
                    if (Interlocked.Exchange(ref injectFailure, 0) != 1) return;
                    Assert.False(File.Exists(path));
                    Assert.Null(await DB.Collection<BlobObject>().Find(x => x.Id == blob.Id).FirstOrDefaultAsync());
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument
                    {
                        { "configureFailPoint", "failCommand" },
                        { "mode", new BsonDocument("times", 1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { "update" } }, { "errorCode", 2 } } }
                    });
                };
                return work;
            }).BuildServiceProvider();
        try
        {
            var error = await Assert.ThrowsAsync<MongoCommandException>(() =>
                BackupsExecution.PruneAsync(services, fixture.DatabaseName, 0, CancellationToken.None));
            Assert.Equal(2, error.Code);
            Assert.Equal(0, injectFailure);
            var stored = await DB.Collection<Backup>().Find(x => x.Id == current.Id).SingleAsync();
            Assert.Equal(BackupStatus.Succeeded, stored.Status);
            Assert.Equal(blob.Id, stored.BlobObjectId);
            Assert.StartsWith("Archive cleanup:", stored.ErrorMessage);
            Assert.False(File.Exists(path));
            Assert.Null(await DB.Collection<BlobObject>().Find(x => x.Id == blob.Id).FirstOrDefaultAsync());
            var untouched = await DB.Collection<Backup>().Find(x => x.Id == older.Id).SingleAsync();
            Assert.Equal(BackupStatus.Succeeded, untouched.Status);
            Assert.Null(untouched.ErrorMessage);
            Assert.True(File.Exists(olderPath));
        }
        finally
        {
            Interlocked.Exchange(ref injectFailure, 0);
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument
            {
                { "configureFailPoint", "failCommand" }, { "mode", "off" }
            });
            await current.ExpireAsync();
            await older.ExpireAsync();
        }
        var completed = await DB.Collection<Backup>().Find(x => x.Id == current.Id).SingleAsync();
        Assert.Equal(BackupStatus.Expired, completed.Status);
        Assert.Null(completed.BlobObjectId);
        Assert.Null(completed.ErrorMessage);
    }

    [Fact]
    public async Task EarlierSaveHookFailureCancelsAndJoinsTheBackupTask()
    {
        using var scope = fixture.Services.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        work.PreSaveChanges += () => throw new IOException("earlier callback failed");
        var backup = new Backup(work, NextSlot());
        await Assert.ThrowsAsync<IOException>(() => work.SaveChanges());
        await backup.CompensateAsync(BackupStatus.Failed, "Earlier save callback failed.");
        var stored = await DB.Collection<Backup>().Find(x => x.Id == backup.Id).FirstOrDefaultAsync();
        Assert.True(stored == null || stored.Status is BackupStatus.Failed or BackupStatus.Cancelled);
        Assert.False(Directory.Exists(Path.Combine(fixture.Options.WorkingDirectory, backup.Id)));
    }

    private sealed class BlockingStream : Stream
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Disposed { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 1024;
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

using System.Security.Cryptography;
using Geex.Extensions.BlobStorage.Core.Entities;
using Geex.Extensions.BlobStorage.Core.Handlers;
using Geex.Extensions.BlobStorage.Requests;
using Geex.Storage;
using HotChocolate.Types;
using MongoDB.Driver;
using MongoDB.Entities;
using Xunit;

namespace Geex.Extensions.BlobStorage.Tests;

[Collection("BlobStorage")]
public class BlobUploadTests(BlobFixture fixture)
{
    private static byte[] Bytes() => Guid.NewGuid().ToByteArray();
    private static string Name() => Guid.NewGuid().ToString("N") + ".txt";
    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(MD5.HashData(bytes));

    private async Task<BlobObject> Upload(string name, byte[] bytes, string storage = "FileSystem",
        string? mime = null, bool includeChecksum = true)
    {
        using var work = new GeexDbContext(fixture.Services);
        var blob = (BlobObject)await new BlobObjectHandler(work).Handle(new CreateBlobObjectRequest
        {
            File = new StreamFile(name, () => new MemoryStream(bytes), bytes.Length, mime),
            StorageType = BlobStorageType.FromValue(storage), Md5 = includeChecksum ? Hash(bytes) : null
        }, CancellationToken.None);
        await work.SaveChanges();
        return blob;
    }

    private async Task<byte[]> Read(BlobObject blob)
    {
        using var work = new GeexDbContext(fixture.Services);
        work.Attach(blob);
        await using var source = await blob.StreamFromStorage();
        using var result = new MemoryStream();
        await source.CopyToAsync(result);
        return result.ToArray();
    }

    [Theory]
    [InlineData("Db")]
    [InlineData("FileSystem")]
    public async Task OrdinaryCreateReturnsNewIdsEvenForIdenticalAttachments(string storage)
    {
        var bytes = Bytes();
        var name = Name();
        var first = await Upload(name, bytes, storage, " TEXT/PLAIN ");
        var second = await Upload(name, bytes, storage, "text/plain");
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal("text/plain", first.MimeType);
        Assert.Equal(first.Md5, second.Md5);
        Assert.Equal(bytes, await Read(second));
        Assert.Equal(2, await DB.Collection<BlobObject>().CountDocumentsAsync(x => x.FileName == name));
        if (storage == "Db")
            Assert.Equal(1, await DB.Collection<DbFile>().CountDocumentsAsync(x => x.Md5 == Hash(bytes)));
    }

    [Theory]
    [InlineData("Db")]
    [InlineData("FileSystem")]
    public async Task UploadWithoutClientChecksumStillComputesAndStoresContent(string storage)
    {
        var bytes = Bytes();
        var blob = await Upload(Name(), bytes, storage, includeChecksum: false);
        Assert.Equal(Hash(bytes), blob.Md5);
        Assert.Equal(bytes.Length, blob.FileSize);
        Assert.Equal("text/plain", blob.MimeType);
        Assert.Equal(bytes, await Read(blob));
        var persisted = await DB.Collection<BlobObject>().Find(x => x.Id == blob.Id).SingleAsync();
        Assert.Equal(Hash(bytes), persisted.Md5);
        Assert.NotEqual(DateTimeOffset.MinValue, persisted.CreatedOn);
    }

    [Fact]
    public async Task IncompleteLegacyDbFileIsSkippedDuringUploadAndDownload()
    {
        var bytes = Bytes();
        using var work = new GeexDbContext(fixture.Services);
        var failed = work.Attach(new DbFile(Hash(bytes)));
        await failed.SaveAsync();
        var blob = await Upload(Name(), bytes, "Db");
        Assert.Equal(bytes, await Read(blob));
        Assert.Equal(2, await DB.Collection<DbFile>().CountDocumentsAsync(x => x.Md5 == Hash(bytes)));
    }

    [Fact]
    public async Task CacheUploadsKeepIndependentFiveMinuteLifetimes()
    {
        var bytes = Bytes();
        var name = Name();
        var started = DateTimeOffset.Now;
        var first = await Upload(name, bytes, "Cache");
        var second = await Upload(name, bytes, "Cache");
        try
        {
            Assert.NotEqual(first.Id, second.Id);
            Assert.InRange(first.ExpireAt!.Value, started.AddMinutes(5), DateTimeOffset.Now.AddMinutes(5));
            Assert.InRange(second.ExpireAt!.Value, started.AddMinutes(5), DateTimeOffset.Now.AddMinutes(5));
            Assert.Equal(bytes, await Read(second));
        }
        finally
        {
            File.Delete(Path.Combine(Path.GetTempPath(), first.Id));
            File.Delete(Path.Combine(Path.GetTempPath(), second.Id));
        }
    }

    [Theory]
    [InlineData("Db")]
    [InlineData("FileSystem")]
    public async Task ExplicitDeletionDeletesTheSharedBlobResource(string storage)
    {
        var blob = await Upload(Name(), Bytes(), storage);
        using var work = new GeexDbContext(fixture.Services);
        var sharedReference = work.Query<BlobObject>().Single(x => x.Id == blob.Id);
        await new BlobObjectHandler(work).Handle(new DeleteBlobObjectRequest
        { Ids = [blob.Id], StorageType = blob.StorageType }, CancellationToken.None);
        Assert.Equal(0, await DB.Collection<BlobObject>().CountDocumentsAsync(x => x.Id == blob.Id));
        await Assert.ThrowsAnyAsync<Exception>(() => Read(sharedReference));
    }

    [Theory]
    [InlineData("Db")]
    [InlineData("FileSystem")]
    public async Task DifferentBlobsRetainSharedContentUntilLastResourceIsDeleted(string storage)
    {
        var bytes = Bytes();
        var first = await Upload(Name(), bytes, storage);
        var second = await Upload(Name(), bytes, storage);
        using var work = new GeexDbContext(fixture.Services);
        await work.Attach(first).DeleteAsync();
        Assert.Equal(bytes, await Read(second));
        await work.Attach(second).DeleteAsync();
        await Assert.ThrowsAnyAsync<Exception>(() => Read(second));
    }

    [Theory]
    [InlineData("Db")]
    [InlineData("FileSystem")]
    public async Task DeletingLastBlobOnlyCountsReferencesInTheSameStorage(string storage)
    {
        var bytes = Bytes();
        var first = await Upload(Name(), bytes, storage);
        var other = await Upload(Name(), bytes, storage == "Db" ? "FileSystem" : "Db");
        using var work = new GeexDbContext(fixture.Services);
        await work.Attach(first).DeleteAsync();
        if (storage == "Db")
            Assert.Equal(0, await DB.Collection<DbFile>().CountDocumentsAsync(x => x.Md5 == first.Md5));
        else
            Assert.False(File.Exists(Path.Combine(fixture.BlobDirectory, first.Md5!)));
        Assert.Equal(bytes, await Read(other));
    }

    [Fact]
    public async Task DeletingLastDbBlobAlsoCleansIncompleteLegacyFiles()
    {
        var bytes = Bytes();
        using var work = new GeexDbContext(fixture.Services);
        var incomplete = work.Attach(new DbFile(Hash(bytes)));
        await incomplete.SaveAsync();
        var blob = await Upload(Name(), bytes, "Db");
        Assert.Equal(2, await DB.Collection<DbFile>().CountDocumentsAsync(x => x.Md5 == blob.Md5));
        await work.Attach(blob).DeleteAsync();
        Assert.Equal(0, await DB.Collection<DbFile>().CountDocumentsAsync(x => x.Md5 == blob.Md5));
        await Assert.ThrowsAnyAsync<Exception>(() => Read(blob));
    }

    [Fact]
    public async Task CancelledUploadDoesNotOpenTheInputStream()
    {
        using var work = new GeexDbContext(fixture.Services);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new BlobObjectHandler(work).Handle(
            new CreateBlobObjectRequest
            {
                File = new StreamFile(Name(), () => throw new InvalidOperationException("Input must not be opened.")),
                StorageType = BlobStorageType.FileSystem
            }, cancellation.Token));
    }

    [Fact]
    public async Task UploadDoesNotCommitOtherBusinessChangesInCaller()
    {
        var existing = await Upload(Name(), Bytes());
        using var work = new GeexDbContext(fixture.Services);
        var tracked = work.Query<BlobObject>().Single(x => x.Id == existing.Id);
        tracked.FileName = "unsaved-business-change.txt";
        var created = (BlobObject)await new BlobObjectHandler(work).Handle(new CreateBlobObjectRequest
        { File = new StreamFile(Name(), () => new MemoryStream(Bytes())), StorageType = BlobStorageType.FileSystem },
            CancellationToken.None);
        await created.WaitForStorageAsync();
        var stored = await DB.Collection<BlobObject>().Find(x => x.Id == existing.Id).SingleAsync();
        Assert.Equal(existing.FileName, stored.FileName);
        Assert.Same(work, ((IEntityBase)created).DbContext);
    }

    [Fact]
    public async Task ConcurrentStreamProcessingKeepsBuffersIndependentAcrossAwaits()
    {
        await Task.WhenAll(Enumerable.Range(1, 8).Select(async value =>
        {
            var bytes = Enumerable.Repeat((byte)value, 150000).ToArray();
            using var source = new MemoryStream(bytes);
            using var destination = new MemoryStream();
            using var hash = MD5.Create();
            var actualHash = await BlobObject.ProcessStreamAsync(source, 65536, hash, async chunk =>
            {
                await Task.Yield();
                await destination.WriteAsync(chunk);
            }, CancellationToken.None);
            Assert.Equal(Hash(bytes), actualHash);
            Assert.Equal(bytes, destination.ToArray());
        }));
    }
}

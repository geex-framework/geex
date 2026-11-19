using System.Security.Cryptography;
using Geex.Extensions.BlobStorage.Core.Entities;
using Geex.Extensions.BlobStorage.Core.Handlers;
using Geex.Extensions.BlobStorage.Requests;
using Geex.Requests;
using Geex.Storage;
using HotChocolate.Types;
using MongoDB.Driver;
using MongoDB.Entities;
using Xunit;

namespace Geex.Extensions.BlobStorage.Tests;

[Collection("BlobStorage")]
public class BlobUploadReadinessTests(BlobFixture fixture)
{
    private async Task<IBlobObject?> FindVisible(string id)
    {
        using var reader = new GeexDbContext(fixture.Services);
        MediatR.IRequestHandler<QueryRequest<IBlobObject>, IQueryable<IBlobObject>> handler = new BlobObjectHandler(reader);
        var query = await handler.Handle(new QueryRequest<IBlobObject>(x => x.Id == id), CancellationToken.None);
        return query.SingleOrDefault();
    }

    private static CreateBlobObjectRequest Request(byte[] bytes, Stream stream, string storage) => new()
    {
        File = new StreamFile(Guid.NewGuid() + ".txt", () => stream, bytes.Length, "text/plain"),
        Md5 = Convert.ToHexStringLower(MD5.HashData(bytes)),
        StorageType = BlobStorageType.FromValue(storage)
    };

    [Theory]
    [InlineData("Db")]
    [InlineData("FileSystem")]
    public async Task PublicQueriesOnlyExposeCompletedAndSavedUploads(string storage)
    {
        var bytes = Guid.NewGuid().ToByteArray();
        using var work = new GeexDbContext(fixture.Services);
        var stream = new GatedStream(bytes);
        var blob = (BlobObject)await new BlobObjectHandler(work).Handle(Request(bytes, stream, storage), CancellationToken.None);
        try
        {
            await stream.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Null(await FindVisible(blob.Id));
            var pending = await DB.Collection<BlobObject>().Find(x => x.Id == blob.Id).SingleAsync();
            Assert.True(pending.UploadPending);
        }
        finally { stream.Release.TrySetResult(); }

        await blob.WaitForStorageAsync().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Null(await FindVisible(blob.Id));
        await work.SaveChanges();
        Assert.Equal(blob.Id, (await FindVisible(blob.Id))?.Id);
    }

    [Theory]
    [InlineData("Db")]
    [InlineData("FileSystem")]
    public async Task FailedUploadsRemainHiddenFromAttachmentQueries(string storage)
    {
        var bytes = Guid.NewGuid().ToByteArray();
        using var work = new GeexDbContext(fixture.Services);
        var stream = new GatedStream(bytes) { FailRead = true };
        var blob = (BlobObject)await new BlobObjectHandler(work).Handle(Request(bytes, stream, storage), CancellationToken.None);
        stream.Release.TrySetResult();
        await Assert.ThrowsAsync<IOException>(() => blob.WaitForStorageAsync());
        Assert.Null(await FindVisible(blob.Id));
        var pending = await DB.Collection<BlobObject>().Find(x => x.Id == blob.Id).SingleAsync();
        Assert.True(pending.UploadPending);
        await blob.DeleteAsync();
    }

    [Fact]
    public async Task LegacyUploadsWithoutPendingMarkerRemainVisibleAndQueryFiltersStillApply()
    {
        var bytes = Guid.NewGuid().ToByteArray();
        using var work = new GeexDbContext(fixture.Services);
        var blob = (BlobObject)await new BlobObjectHandler(work).Handle(
            Request(bytes, new MemoryStream(bytes), "FileSystem"), CancellationToken.None);
        await work.SaveChanges();
        await DB.Collection<BlobObject>().UpdateOneAsync(x => x.Id == blob.Id,
            Builders<BlobObject>.Update.Unset(x => x.UploadPending));
        Assert.Equal(blob.Id, (await FindVisible(blob.Id))?.Id);
        Assert.Null(await FindVisible(global::MongoDB.Bson.ObjectId.GenerateNewId().ToString()));
    }

    private sealed class GatedStream(byte[] bytes) : MemoryStream(bytes)
    {
        public bool FailRead { get; init; }
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private async Task BeforeRead(CancellationToken cancellationToken)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            if (FailRead) throw new IOException("Injected upload failure.");
        }

        public override int Read(Span<byte> buffer)
        {
            BeforeRead(CancellationToken.None).GetAwaiter().GetResult();
            return base.Read(buffer);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            BeforeRead(CancellationToken.None).GetAwaiter().GetResult();
            return base.Read(buffer, offset, count);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await BeforeRead(cancellationToken);
            return await base.ReadAsync(buffer, cancellationToken);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await BeforeRead(cancellationToken);
            return await base.ReadAsync(buffer, offset, count, cancellationToken);
        }
    }
}

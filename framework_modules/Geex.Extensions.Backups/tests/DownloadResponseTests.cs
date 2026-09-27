using System.IO.Compression;
using System.Net;
using System.Text;
using Geex.Extensions.BlobStorage;
using Geex.Extensions.BlobStorage.Core.Entities;
using Geex.Extensions.BlobStorage.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Geex.Extensions.Backups.Tests;

public class DownloadResponseTests
{
    [Theory]
    [InlineData("document.txt", "text/plain")]
    [InlineData("backup.archive.gz", "application/gzip")]
    public async Task SuccessfulDownloadsPreserveBytesHeadersAndDisposeTheStream(string name, string mimeType)
    {
        var data = Encoding.UTF8.GetBytes("isolated download verification");
        if (name.EndsWith(".gz"))
        {
            using var archive = new MemoryStream();
            using (var gzip = new GZipStream(archive, CompressionMode.Compress, leaveOpen: true)) gzip.Write(data);
            data = archive.ToArray();
        }
        var source = new ControlledStream(data);
        var blob = TestBlob.Create(data.LongLength, _ => Task.FromResult<Stream>(source), name, mimeType);
        await using var server = await DownloadServer.Start(_ => blob);

        using var response = await server.Client.GetAsync("/download?fileId=test");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(data, await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(data.LongLength, response.Content.Headers.ContentLength);
        Assert.Equal(name, response.Content.Headers.ContentDisposition!.FileNameStar);
        Assert.Equal(mimeType, response.Content.Headers.ContentType!.MediaType);
        await source.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("/download", HttpStatusCode.BadRequest)]
    [InlineData("/download?fileId=", HttpStatusCode.BadRequest)]
    [InlineData("/download?fileId=%20", HttpStatusCode.BadRequest)]
    [InlineData("/download?fileId=missing", HttpStatusCode.NotFound)]
    public async Task MissingParametersAndMetadataDoNotReturnAnAttachment(string path, HttpStatusCode status)
    {
        await using var server = await DownloadServer.Start(_ => null);
        using var response = await server.Client.GetAsync(path);
        Assert.Equal(status, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentDisposition);
    }

    [Fact]
    public async Task OpeningAMissingArchiveReturnsAnErrorWithoutDownloadHeaders()
    {
        var blob = TestBlob.Create(123, _ => throw new FileNotFoundException("isolated missing archive"));
        await using var server = await DownloadServer.Start(_ => blob);
        using var response = await server.Client.GetAsync("/download?fileId=test");
        await AssertFailureResponse(response);
    }

    [Fact]
    public async Task FirstReadFailureCanStillReturnANonSuccessResponse()
    {
        var source = new ControlledStream([1, 2, 3], failAfterReads: 0);
        var blob = TestBlob.Create(3, _ => Task.FromResult<Stream>(source));
        await using var server = await DownloadServer.Start(_ => blob);
        using var response = await server.Client.GetAsync("/download?fileId=test");
        await AssertFailureResponse(response);
        await source.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PartialOrShortArchivesCannotCompleteAsSuccessfulDownloads(bool shortRead)
    {
        var source = new ControlledStream([1, 2, 3], failAfterReads: shortRead ? -1 : 1, chunkSize: 1);
        var blob = TestBlob.Create(10, _ => Task.FromResult<Stream>(source));
        await using var server = await DownloadServer.Start(_ => blob);
        await Assert.ThrowsAnyAsync<HttpRequestException>(() => server.Client.GetByteArrayAsync("/download?fileId=test"));
        await source.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ImmediateEndOfStreamCannotCompleteANonemptyArchiveDownload()
    {
        var source = new ControlledStream([]);
        var blob = TestBlob.Create(10, _ => Task.FromResult<Stream>(source));
        await using var server = await DownloadServer.Start(_ => blob);
        await Assert.ThrowsAnyAsync<HttpRequestException>(() => server.Client.GetByteArrayAsync("/download?fileId=test"));
        await source.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ClientCancellationCancelsTheReadAndDisposesTheStream()
    {
        var source = new ControlledStream([1, 2, 3], chunkSize: 1, waitAfterFirstRead: true);
        var blob = TestBlob.Create(3, token =>
        {
            Assert.True(token.CanBeCanceled);
            return Task.FromResult<Stream>(source);
        });
        await using var server = await DownloadServer.Start(_ => blob);
        using var cancellation = new CancellationTokenSource();
        var request = server.Client.GetByteArrayAsync("/download?fileId=test", cancellation.Token);
        await source.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        await source.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(source.ReadCancelled);
    }

    private static async Task AssertFailureResponse(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentDisposition);
        Assert.Null(response.Headers.CacheControl);
        Assert.Equal("File download failed.", await response.Content.ReadAsStringAsync());
    }

    private sealed class TestBlob : BlobObject, IBlobObject
    {
        private Func<CancellationToken, Task<Stream>> _open = null!;

        public static IBlobObject Create(long size, Func<CancellationToken, Task<Stream>> open,
            string name = "backup.archive.gz", string mimeType = "application/gzip")
        {
            return new TestBlob { FileSize = size, FileName = name, MimeType = mimeType,
                Md5 = "test-checksum", _open = open };
        }

        Task<Stream> IBlobObject.StreamFromStorage(CancellationToken cancellationToken) => _open(cancellationToken);
    }

    private sealed class DownloadServer(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public static async Task<DownloadServer> Start(Func<string, IBlobObject?> findBlob)
        {
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
            { EnvironmentName = "Test", ContentRootPath = AppContext.BaseDirectory });
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
            var app = builder.Build();
            app.Map("/download", context => MicrosoftAspNetCoreBuilderExtension.DownloadAsync(context, findBlob));
            await app.StartAsync();
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            return new DownloadServer(app, new HttpClient(new HttpClientHandler { UseProxy = false })
            { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(10) });
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private sealed class ControlledStream(byte[] data, int failAfterReads = -1, int chunkSize = int.MaxValue,
        bool waitAfterFirstRead = false) : Stream
    {
        private int _offset;
        private int _reads;
        public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Waiting { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool ReadCancelled { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => data.LongLength;
        public override long Position { get => _offset; set => throw new NotSupportedException(); }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_reads == failAfterReads) throw new IOException("isolated read failure");
            if (waitAfterFirstRead && _reads > 0)
            {
                Waiting.TrySetResult();
                try { await Task.Delay(Timeout.Infinite, cancellationToken); }
                catch (OperationCanceledException) { ReadCancelled = true; throw; }
            }
            var count = Math.Min(Math.Min(buffer.Length, chunkSize), data.Length - _offset);
            data.AsMemory(_offset, count).CopyTo(buffer);
            _offset += count;
            _reads++;
            return count;
        }

        protected override void Dispose(bool disposing) { Disposed.TrySetResult(); base.Dispose(disposing); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

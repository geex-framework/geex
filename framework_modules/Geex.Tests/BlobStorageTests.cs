using Geex.Common;
using Geex.Common.Abstraction.Entities;
using Geex.Common.BlobStorage.Api.Abstractions;
using Geex.Common.Requests.BlobStorage;
using HotChocolate.Types;

using MediatR;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Geex.Tests
{
    public class BlobStorageTests : IClassFixture<GeexWebApplicationFactory>
    {
        private readonly GeexWebApplicationFactory _factory;

        public BlobStorageTests(GeexWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task MemoryFileUploadShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var dateTime = DateTime.Now;
            var data = new byte[1024 * 2048];
            Array.Fill<byte>(data, 1);

            // Act
            var blob = await uow.Request(new CreateBlobObjectRequest() { File = new StreamFile($"{dateTime}.txt", () => new MemoryStream(data)), StorageType = BlobStorageType.Db });
            await uow.SaveChanges();
            // Assert
            using var service1 = service.CreateScope();
            var file = await service1.ServiceProvider.GetService<IMediator>().Send(new DownloadFileRequest(blob.Id));
            file.dataStream.Length.ShouldBe(data.Length);
            file.blob.FileSize.ShouldBe(data.Length);
        }
    }
}

using Geex.Extensions.BlobStorage;
using Geex.Extensions.BlobStorage.Requests;

using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(FeatureTestsCollection))]
    public class BlobStorageTests
    {
        private readonly FeatureTestApplicationFactory _factory;

        public BlobStorageTests(FeatureTestApplicationFactory factory)
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
            var file = service1.ServiceProvider.GetService<IUnitOfWork>().Query<IBlobObject>().GetById(blob.Id);
            var dataStream = await file.StreamFromStorage();
            dataStream.Length.ShouldBe(data.Length);
            file.FileSize.ShouldBe(data.Length);
        }
    }
}

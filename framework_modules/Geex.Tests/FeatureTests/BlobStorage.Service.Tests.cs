using Geex.Extensions.BlobStorage;
using Geex.Extensions.BlobStorage.Requests;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Types;
using MongoDB.Bson;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class BlobStorageServiceTests
    {
        private readonly TestApplicationFactory _factory;

        public BlobStorageServiceTests(TestApplicationFactory factory)
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

        [Fact]
        public async Task CreateBlobObjectServiceShouldWork()
        {
            // Arrange
            var testFileName = $"test_{ObjectId.GenerateNewId()}.txt";
            var testData = "test file content for blob storage";
            var fileBytes = Encoding.UTF8.GetBytes(testData);
            var md5Hash = System.Security.Cryptography.MD5.HashData(fileBytes);
            var md5String = Convert.ToHexString(md5Hash).ToLower();

            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();

            // Act
            var blob = await uow.Request(new CreateBlobObjectRequest()
            {
                File = new StreamFile(testFileName, () => new MemoryStream(fileBytes)),
                StorageType = BlobStorageType.Db,
                Md5 = md5String
            });
            await uow.SaveChanges();

            // Assert
            blob.ShouldNotBeNull();
            blob.FileName.ShouldBe(testFileName);
            blob.FileSize.ShouldBe(fileBytes.Length);
            blob.Md5.ShouldBe(md5String);
            blob.StorageType.ShouldBe(BlobStorageType.Db);
        }

        [Fact]
        public async Task DeleteBlobObjectServiceShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();

            var testFileName = $"delete_test_{ObjectId.GenerateNewId()}.txt";
            var testData = "content to be deleted";
            var fileBytes = Encoding.UTF8.GetBytes(testData);

            var blob = await uow.Request(new CreateBlobObjectRequest()
            {
                File = new StreamFile(testFileName, () => new MemoryStream(fileBytes)),
                StorageType = BlobStorageType.Db
            });
            await uow.SaveChanges();

            // Act
            await uow.Request(new DeleteBlobObjectRequest() { Ids = [blob.Id], StorageType = BlobStorageType.Db });
            await uow.SaveChanges();

            // Assert
            using var verifyService = service.CreateScope();
            var verifyUow = verifyService.ServiceProvider.GetService<IUnitOfWork>();
            var deletedBlob = verifyUow.Query<IBlobObject>().FirstOrDefault(x => x.Id == blob.Id);
            deletedBlob.ShouldBeNull();
        }

        [Fact]
        public async Task FileSystemStorageTypeShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var testFileName = $"filesystem_test_{ObjectId.GenerateNewId()}.txt";
            var testData = "test content for filesystem storage";
            var fileBytes = Encoding.UTF8.GetBytes(testData);

            // Act
            var blob = await uow.Request(new CreateBlobObjectRequest()
            {
                File = new StreamFile(testFileName, () => new MemoryStream(fileBytes)),
                StorageType = BlobStorageType.FileSystem
            });
            await uow.SaveChanges();

            // Assert
            blob.ShouldNotBeNull();
            blob.StorageType.ShouldBe(BlobStorageType.FileSystem);
            blob.FileSize.ShouldBe(fileBytes.Length);

            // Verify we can read the content back
            using var service1 = service.CreateScope();
            var file = service1.ServiceProvider.GetService<IUnitOfWork>().Query<IBlobObject>().GetById(blob.Id);
            var dataStream = await file.StreamFromStorage();
            dataStream.Length.ShouldBe(fileBytes.Length);
        }

        [Fact]
        public async Task BlobObjectUrlShouldBeGenerated()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var testFileName = $"url_test_{ObjectId.GenerateNewId()}.txt";
            var testData = "test content for url generation";
            var fileBytes = Encoding.UTF8.GetBytes(testData);

            // Act
            var blob = await uow.Request(new CreateBlobObjectRequest()
            {
                File = new StreamFile(testFileName, () => new MemoryStream(fileBytes)),
                StorageType = BlobStorageType.Db
            });
            await uow.SaveChanges();

            // Assert
            blob.Url.ShouldNotBeNullOrEmpty();
            blob.Url.ShouldContain(blob.Id);
            blob.Url.ShouldContain("/download");
        }

        [Fact]
        public async Task LargeFileUploadShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var testFileName = $"large_file_{ObjectId.GenerateNewId()}.txt";

            // Create a larger file (5MB)
            var largeData = new byte[5 * 1024 * 1024];
            Array.Fill<byte>(largeData, 42);

            // Act
            var blob = await uow.Request(new CreateBlobObjectRequest()
            {
                File = new StreamFile(testFileName, () => new MemoryStream(largeData)),
                StorageType = BlobStorageType.Db
            });
            await uow.SaveChanges();

            // Assert
            blob.ShouldNotBeNull();
            blob.FileSize.ShouldBe(largeData.Length);

            // Verify we can read the content back
            using var service1 = service.CreateScope();
            var file = service1.ServiceProvider.GetService<IUnitOfWork>().Query<IBlobObject>().GetById(blob.Id);
            var dataStream = await file.StreamFromStorage();
            dataStream.Length.ShouldBe(largeData.Length);
        }
    }
}

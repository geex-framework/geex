using Geex.Extensions.BlobStorage;
using Geex.Extensions.BlobStorage.Requests;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Types;
using MongoDB.Bson;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class BlobStorageServiceTests : TestsBase
    {
        public BlobStorageServiceTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task MemoryFileUploadShouldWork()
        {
            // Arrange
            var dateTime = DateTime.Now;
            var data = new byte[1024 * 2048];
            Array.Fill<byte>(data, 1);
            string blobId;

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var blob = await uow.Request(new CreateBlobObjectRequest() { File = new StreamFile($"{dateTime}.txt", () => new MemoryStream(data)), StorageType = BlobStorageType.Db });
                await uow.SaveChanges();
                blobId = blob.Id;
            }

            // Assert
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var file = verifyUow.Query<IBlobObject>().GetById(blobId);
                var dataStream = await file.StreamFromStorage();
                dataStream.Length.ShouldBe(data.Length);
                file.FileSize.ShouldBe(data.Length);
            }
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

            // Act
            IBlobObject blob;
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                blob = await uow.Request(new CreateBlobObjectRequest()
                {
                    File = new StreamFile(testFileName, () => new MemoryStream(fileBytes)),
                    StorageType = BlobStorageType.Db,
                    Md5 = md5String
                });
                await uow.SaveChanges();
            }

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
            var testFileName = $"delete_test_{ObjectId.GenerateNewId()}.txt";
            var testData = "content to be deleted";
            var fileBytes = Encoding.UTF8.GetBytes(testData);
            string blobId;

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var blob = await uow.Request(new CreateBlobObjectRequest()
                {
                    File = new StreamFile(testFileName, () => new MemoryStream(fileBytes)),
                    StorageType = BlobStorageType.Db
                });
                await uow.SaveChanges();
                blobId = blob.Id;
            }

            // Act
            using (var deleteScope = ScopedService.CreateScope())
            {
                var deleteUow = deleteScope.ServiceProvider.GetService<IUnitOfWork>();
                await deleteUow.Request(new DeleteBlobObjectRequest() { Ids = [blobId], StorageType = BlobStorageType.Db });
                await deleteUow.SaveChanges();
            }

            // Assert
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var deletedBlob = verifyUow.Query<IBlobObject>().FirstOrDefault(x => x.Id == blobId);
                deletedBlob.ShouldBeNull();
            }
        }

        [Fact]
        public async Task FileSystemStorageTypeShouldWork()
        {
            // Arrange
            var testFileName = $"filesystem_test_{ObjectId.GenerateNewId()}.txt";
            var testData = "test content for filesystem storage";
            var fileBytes = Encoding.UTF8.GetBytes(testData);
            string blobId;

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var blob = await uow.Request(new CreateBlobObjectRequest()
                {
                    File = new StreamFile(testFileName, () => new MemoryStream(fileBytes)),
                    StorageType = BlobStorageType.FileSystem
                });
                await uow.SaveChanges();
                blobId = blob.Id;
            }

            // Assert
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var file = verifyUow.Query<IBlobObject>().GetById(blobId);
                file.ShouldNotBeNull();
                file.StorageType.ShouldBe(BlobStorageType.FileSystem);
                file.FileSize.ShouldBe(fileBytes.Length);

                // Verify we can read the content back
                var dataStream = await file.StreamFromStorage();
                dataStream.Length.ShouldBe(fileBytes.Length);
            }
        }

        [Fact]
        public async Task BlobObjectUrlShouldBeGenerated()
        {
            // Arrange
            var testFileName = $"url_test_{ObjectId.GenerateNewId()}.txt";
            var testData = "test content for url generation";
            var fileBytes = Encoding.UTF8.GetBytes(testData);

            string blobId;
            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var blob = await uow.Request(new CreateBlobObjectRequest()
                {
                    File = new StreamFile(testFileName, () => new MemoryStream(fileBytes)),
                    StorageType = BlobStorageType.Db
                });
                blobId = blob.Id;
                await uow.SaveChanges();
            }

            // Assert
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var blob = uow.Query<IBlobObject>().First(x=>x.Id == blobId);
                blob.Url.ShouldNotBeNullOrEmpty();
                blob.Url.ShouldContain(blob.Id);
                blob.Url.ShouldContain("/download");
            }
        }

        [Fact]
        public async Task LargeFileUploadShouldWork()
        {
            // Arrange
            var testFileName = $"large_file_{ObjectId.GenerateNewId()}.txt";
            // Create a larger file (128MB)
            var largeData = new byte[128 * 1024 * 1024];
            Array.Fill<byte>(largeData, 42);
            string blobId;

            var st = Stopwatch.StartNew();

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var blob = await uow.Request(new CreateBlobObjectRequest()
                {
                    File = new StreamFile(testFileName, () => new MemoryStream(largeData)),
                    StorageType = BlobStorageType.Db
                });
                await uow.SaveChanges();
                blobId = blob.Id;
            }

            st.Stop();
            var uploadTimeCost = st.ElapsedMilliseconds;
            Debug.WriteLine($"Large file upload took: {uploadTimeCost} ms");

            // Assert
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var file = verifyUow.Query<IBlobObject>().GetById(blobId);
                file.ShouldNotBeNull();
                file.FileSize.ShouldBe(largeData.Length);

                // Verify we can read the content back
                var dataStream = await file.StreamFromStorage();
                dataStream.Length.ShouldBe(largeData.Length);
            }
            uploadTimeCost.ShouldBeLessThan(2000);
        }
    }
}

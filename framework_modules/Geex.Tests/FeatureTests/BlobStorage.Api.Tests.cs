using Geex.Extensions.BlobStorage;
using Geex.Extensions.BlobStorage.Requests;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HotChocolate.Types;
using MongoDB.Bson;

using Newtonsoft.Json;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class BlobStorageApiTests : TestsBase
    {
        public BlobStorageApiTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task QueryBlobObjectsShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var query = """
                query {
                    blobObjects(skip: 0, take: 10) {
                        items { id fileName fileSize storageType url }
                        pageInfo { hasPreviousPage hasNextPage }
                        totalCount
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(GqlEndpoint, query);

            // Assert
            int totalCount = responseData["data"]["blobObjects"]["totalCount"].GetValue<int>();
            totalCount.ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task FilterBlobObjectsByFileNameShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var targetFileName = $"specific_file_{ObjectId.GenerateNewId()}.txt";

            // Prepare data using separate scope
            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var testData = "test content for filtering";
                var fileBytes = Encoding.UTF8.GetBytes(testData);

                await setupUow.Request(new CreateBlobObjectRequest()
                {
                    File = new StreamFile(targetFileName, () => new MemoryStream(fileBytes)),
                    StorageType = BlobStorageType.Db
                });
                await setupUow.SaveChanges();
            }

            var query = """
                query($fileName: String!) {
                    blobObjects(skip: 0, take: 10, filter: { fileName: { eq: $fileName } }) {
                        items { id fileName fileSize storageType }
                        totalCount
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(GqlEndpoint, query, new { fileName = targetFileName });

            // Assert
            var items = responseData["data"]["blobObjects"]["items"].AsArray();
            (items).Count.ShouldBeGreaterThan(0);

            foreach (var item in items)
            {
                ((string)item["fileName"]).ShouldBe(targetFileName);
            }
        }

        [Fact]
        public async Task DeleteBlobObjectMutationShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var testFileName = $"deleteapi_{ObjectId.GenerateNewId()}.txt";
            string blobId;

            // Prepare data using separate scope
            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var testData = "API content to be deleted";
                var fileBytes = Encoding.UTF8.GetBytes(testData);

                var blob = await setupUow.Request(new CreateBlobObjectRequest()
                {
                    File = new StreamFile(testFileName, () => new MemoryStream(fileBytes)),
                    StorageType = BlobStorageType.Db
                });
                await setupUow.SaveChanges();
                blobId = blob.Id;
            }

            var query = """
                mutation($blobId: String!) {
                    deleteBlobObject(request: { ids: [$blobId], storageType: Db })
                }
                """;

            // Act
            var (responseData, _) = await client.PostGqlRequest(GqlEndpoint, query, new { blobId });

            // Assert
            bool deleteResult = (bool)responseData["data"]["deleteBlobObject"];
            deleteResult.ShouldBeTrue();            // Verify the blob is actually deleted
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var deletedBlob = verifyUow.Query<IBlobObject>().FirstOrDefault(x => x.Id == blobId);
                deletedBlob.ShouldBeNull();
            }
        }

        [Fact]
        public async Task FilterBlobObjectsByStorageTypeShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var testFileName = $"storage_filter_test_{ObjectId.GenerateNewId()}.txt";

            // Prepare data using separate scope
            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var testData = "test content for storage type filtering";
                var fileBytes = Encoding.UTF8.GetBytes(testData);

                await setupUow.Request(new CreateBlobObjectRequest()
                {
                    File = new StreamFile(testFileName, () => new MemoryStream(fileBytes)),
                    StorageType = BlobStorageType.Db
                });
                await setupUow.SaveChanges();
            }

            var query = """
                query {
                    blobObjects(skip: 0, take: 10, filter: { storageType: { eq: Db } }) {
                        items { id fileName storageType }
                        totalCount
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(GqlEndpoint, query);

            // Assert
            var items = responseData["data"]["blobObjects"]["items"].AsArray();
            (items).Count.ShouldBeGreaterThan(0);

            foreach (var item in items)
            {
                ((string)item["storageType"]).ShouldBe("Db");
            }
        }
    }
}

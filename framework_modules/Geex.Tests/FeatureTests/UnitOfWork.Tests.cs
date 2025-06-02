using Geex.Tests.FeatureTests.TestEntities;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;

using Shouldly;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class UnitOfWorkTests : TestsBase
    {
        public UnitOfWorkTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task ScopedService_ShouldHaveDifferentUnitOfWorkInstances()
        {
            // Arrange & Act & Assert
            using var scope1 = ScopedService.CreateScope();
            using var scope2 = ScopedService.CreateScope();

            var uow1 = scope1.ServiceProvider.GetService<IUnitOfWork>();
            var uow2 = scope2.ServiceProvider.GetService<IUnitOfWork>();

            uow1.GetHashCode().ShouldNotBeSameAs(uow2.GetHashCode());
        }

        [Fact]
        public async Task SessionTest()
        {
            var client = new MongoClient();
            await client.GetDatabase("tests").DropCollectionAsync("collection1");
            var session1 = await client.StartSessionAsync(DbContext.DefaultSessionOptions);
            var session2 = await client.StartSessionAsync(DbContext.DefaultSessionOptions);
            session1.StartTransaction();
            var collection1 = session1.Client.GetDatabase("tests").GetCollection<BsonDocument>("collection1");
            await collection1.InsertOneAsync(session1, new BsonDocument("_id", 1));
            var result1 = collection1.Find(session1, new BsonDocument()).ToList();
            result1.Count.ShouldBe(1);
            var result2 = collection1.Find(session2, new BsonDocument()).ToList();
            result2.Count.ShouldBe(0);
            await session1.CommitTransactionAsync();

            session2.StartTransaction();
            var result3 = collection1.Find(session2, new BsonDocument()).ToList();
            result3.Count.ShouldBe(1);
            await session2.CommitTransactionAsync();
        }

        [Fact]
        public async Task UnitOfWorkShouldWork()
        {
            // Arrange
            var client = new MongoClient();
            await DB.DefaultDb.DropCollectionAsync(nameof(TestEntity));

            string entity1Id, entity2Id;

            // Act 1 - Create entities in separate scopes
            using (var scope1 = ScopedService.CreateScope())
            {
                var uow1 = scope1.ServiceProvider.GetService<IUnitOfWork>();
                var entity1 = uow1.Attach(new TestEntity()
                {
                    Name = "1",
                    Value = 1,
                    Data = new[] { 1 }
                });
                entity1Id = entity1.Id;

                uow1.Query<TestEntity>().Count().ShouldBe(1);
                await uow1.SaveChanges();
            }

            using (var scope2 = ScopedService.CreateScope())
            {
                var uow2 = scope2.ServiceProvider.GetService<IUnitOfWork>();
                var entity2 = uow2.Attach(new TestEntity()
                {
                    Name = "2",
                    Value = 2,
                    Data = new[] { 2 }
                });
                entity2Id = entity2.Id;

                uow2.Query<TestEntity>().Count().ShouldBe(2);
                // entity attached but not saved to db
                client.GetDatabase("tests").GetCollection<TestEntity>(nameof(TestEntity)).Find(new BsonDocument()).ToList().Count.ShouldBe(1);
                await uow2.SaveChanges();
                // entity saved to db
                client.GetDatabase("tests").GetCollection<TestEntity>(nameof(TestEntity)).Find(new BsonDocument()).ToList().Count.ShouldBe(2);
            }

            // Verify final state
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                verifyUow.Query<TestEntity>().Count().ShouldBe(2);
                client.GetDatabase("tests").GetCollection<TestEntity>(nameof(TestEntity)).Find(new BsonDocument()).ToList().Count.ShouldBe(2);
            }

            // Cleanup
            await DB.DefaultDb.DropCollectionAsync(nameof(TestEntity));
        }
    }
}

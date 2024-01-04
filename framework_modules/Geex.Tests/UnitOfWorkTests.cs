using Geex.Common;
using Geex.Tests.TestEntities;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;

using Shouldly;

namespace Geex.Tests
{
    public class UnitOfWorkTests : IClassFixture<GeexWebApplicationFactory>
    {
        private readonly GeexWebApplicationFactory _factory;

        public UnitOfWorkTests(GeexWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ScopedService_ShouldHaveDifferentUnitOfWorkInstances()
        {
            // Arrange
            var service = _factory.Services;
            var scopedProvider1 = service.CreateScope().ServiceProvider;
            var scopedProvider2 = scopedProvider1.CreateScope().ServiceProvider;

            // Act
            var uow1 = scopedProvider1.GetService<IUnitOfWork>();
            var uow2 = scopedProvider2.GetService<IUnitOfWork>();

            // Assert
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
            var service = _factory.Services;
            var scopedProvider1 = service.CreateScope().ServiceProvider;
            var scopedProvider2 = service.CreateScope().ServiceProvider;
            var uow1 = scopedProvider1.GetService<IUnitOfWork>();
            var uow2 = scopedProvider2.GetService<IUnitOfWork>();

            // Act
            uow1.Attach(new TestEntity()
            {
                Name = "1",
                Value = 1,
                Data = new[] { 1 }
            });
            uow2.Attach(new TestEntity()
            {
                Name = "2",
                Value = 2,
                Data = new[] { 2 }
            });
            //await uow2.CommitAsync();
            scopedProvider1.GetService<IRepository>().ToString().ShouldBeEquivalentTo(uow1.ToString());
            scopedProvider2.GetService<IRepository>().ToString().ShouldBeEquivalentTo(uow2.ToString());
            uow1.Query<TestEntity>().Count().ShouldBe(1);
            uow2.Query<TestEntity>().Count().ShouldBe(1);
            client.GetDatabase("tests").GetCollection<TestEntity>(nameof(TestEntity)).Find(new BsonDocument()).ToList().Count.ShouldBe(0);
            await uow2.SaveChanges();
            client.GetDatabase("tests").GetCollection<TestEntity>(nameof(TestEntity)).Find(new BsonDocument()).ToList().Count.ShouldBe(1);
            uow1.Query<TestEntity>().Count().ShouldBe(2);
            uow2.Query<TestEntity>().Count().ShouldBe(1);
            await uow1.SaveChanges();
            uow1.Query<TestEntity>().Count().ShouldBe(2);
            uow2.Query<TestEntity>().Count().ShouldBe(2);
            client.GetDatabase("tests").GetCollection<TestEntity>(nameof(TestEntity)).Find(new BsonDocument()).ToList().Count.ShouldBe(2);
            service.CreateScope().ServiceProvider.GetService<IRepository>().Query<TestEntity>().Count().ShouldBe(2);
            // Assert
            uow1.GetHashCode().ShouldNotBeSameAs(uow2.GetHashCode());
        }
    }
}

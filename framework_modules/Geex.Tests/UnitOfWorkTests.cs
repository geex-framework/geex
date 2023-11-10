using Geex.Common;
using Geex.Tests.TestEntities;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

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
        public async Task UnitOfWorkShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var scopedProvider1 = service.CreateScope().ServiceProvider;
            var scopedProvider2 = scopedProvider1.CreateScope().ServiceProvider;
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
            scopedProvider1.GetService<IRepository>().Query<TestEntity>().Count().ShouldBe(1);
            scopedProvider2.GetService<IRepository>().Query<TestEntity>().Count().ShouldBe(1);
            await uow2.CommitAsync();
            scopedProvider1.GetService<IRepository>().Query<TestEntity>().Count().ShouldBe(1);
            scopedProvider2.GetService<IRepository>().Query<TestEntity>().Count().ShouldBe(1);
            await uow1.SaveChanges();
            scopedProvider1.GetService<IRepository>().Query<TestEntity>().Count().ShouldBe(1);
            scopedProvider2.GetService<IRepository>().Query<TestEntity>().Count().ShouldBe(2);
            uow1.Query<TestEntity>().Count().ShouldBe(1);
            await uow1.AbortAsync();
            scopedProvider1.GetService<IRepository>().Query<TestEntity>().Count().ShouldBe(1);
            scopedProvider2.GetService<IRepository>().Query<TestEntity>().Count().ShouldBe(1);
            // Assert
            uow1.GetHashCode().ShouldNotBeSameAs(uow2.GetHashCode());
        }
    }
}

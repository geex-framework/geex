using System.Reflection;
using Geex.Common;
using Geex.Common.Abstraction.Entities;
using Geex.Tests.TestEntities;
using HotChocolate.Types;

using MediatR;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Geex.Tests
{
    public class AnalyzerTests : IClassFixture<GeexWebApplicationFactory>
    {
        private readonly GeexWebApplicationFactory _factory;

        public AnalyzerTests(GeexWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task UowCreateShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var dateTime = DateTime.Now;
            var data = new int[1024];
            Array.Fill<int>(data, 1);

            // Act
            var testEntity = uow.Create("test", 1, data, dateTime, dateTime);
            var serviceProviderProperty = testEntity.GetType().GetProperty("ServiceProvider", BindingFlags.Instance | BindingFlags.NonPublic);
            var serviceProvider = serviceProviderProperty.GetValue(testEntity);
            Assert.NotNull(serviceProvider);
        }
    }
}

using System.Reflection;
using Geex.Common;
using Geex.Tests.FeatureTests;
using HotChocolate.Types;

using MediatR;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Geex.Tests
{
    [Collection(nameof(FeatureTestsCollection))]
    public class AnalyzerTests
    {
        private readonly FeatureTestApplicationFactory _factory;

        public AnalyzerTests(FeatureTestApplicationFactory factory)
        {
            _factory = factory;
        }

        //[Fact]
        //public async Task UowCreateShouldWork()
        //{
        //    // Arrange
        //    var service = _factory.Services;
        //    var uow = service.GetService<IUnitOfWork>();
        //    var dateTime = DateTime.Now;
        //    var data = new int[1024];
        //    Array.Fill<int>(data, 1);

        //    // Act
        //    var testEntity = uow.Create("test", 1, data, dateTime, dateTime);
        //    var serviceProviderProperty = testEntity.GetType().GetProperty("ServiceProvider", BindingFlags.Instance | BindingFlags.NonPublic);
        //    var serviceProvider = serviceProviderProperty.GetValue(testEntity);
        //    Assert.NotNull(serviceProvider);
        //}
    }
}

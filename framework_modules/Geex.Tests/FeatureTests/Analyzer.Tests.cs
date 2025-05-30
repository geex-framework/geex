namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class AnalyzerTests
    {
        private readonly TestApplicationFactory _factory;

        public AnalyzerTests(TestApplicationFactory factory)
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

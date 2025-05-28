namespace Geex.Tests
{
    [CollectionDefinition(nameof(TestsCollection))]
    public class TestsCollection : ICollectionFixture<TestApplicationFactory>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}

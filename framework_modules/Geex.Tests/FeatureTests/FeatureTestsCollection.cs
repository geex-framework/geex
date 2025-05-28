namespace Geex.Tests.FeatureTests
{
    [CollectionDefinition(nameof(FeatureTestsCollection))]
    public class FeatureTestsCollection : ICollectionFixture<FeatureTestApplicationFactory>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}

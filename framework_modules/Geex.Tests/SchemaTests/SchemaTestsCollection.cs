namespace Geex.Tests.SchemaTests
{
    [CollectionDefinition(nameof(SchemaTestsCollection))]
    public class SchemaTestsCollection : ICollectionFixture<GeexWebApplicationFactory>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}

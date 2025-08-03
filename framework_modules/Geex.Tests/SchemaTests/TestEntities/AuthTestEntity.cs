using Geex.Storage;

namespace Geex.Tests.SchemaTests.TestEntities
{
    public class TestPermissions : AppPermission<TestPermissions>
    {
        /// <inheritdoc />
        public TestPermissions(string value) : base("test_"+value)
        {

        }
        public static TestPermissions AuthTestQueryField { get; } = new("query_authTestQueryField");

        public static TestPermissions AuthTestEntityAuthorizedField { get; } = new("authTestEntity_authorizedField");

        public static TestPermissions AuthTestMutationField { get; } = new("mutation_authTestMutationField");
    }

    public class AuthTestEntity : Entity<AuthTestEntity>
    {
        public string AuthorizedField => "1";
    }
}

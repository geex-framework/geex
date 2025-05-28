using Geex.Gql.Types;
using Geex.Storage;
using HotChocolate.Types;

namespace Geex.Tests.SchemaTests.TestEntities
{
    public class TestPermissions : AppPermission<TestPermissions>
    {
        /// <inheritdoc />
        public TestPermissions(string value) : base(value)
        {
        }
        public static TestPermissions AuthTestQueryField { get; } = new("test_query_authTestQueryField");

        public static TestPermissions AuthTestEntityAuthorizedField { get; } = new("test_authTestEntity_authorizedField");

        public static TestPermissions AuthTestMutationField { get; } = new("test_mutation_authTestMutationField");
    }

    public class AuthTestEntity : Entity<AuthTestEntity>
    {
        public string AuthorizedField => "1";
    }
}

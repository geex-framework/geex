using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Gql.Types;
using Geex.Storage;

using HotChocolate.Types;

namespace Geex.Tests.TestEntities
{
    public class TestPermissions : AppPermission<TestPermissions>
    {
        /// <inheritdoc />
        public TestPermissions(string value) : base(value)
        {
        }
        public static TestPermissions Query => new("test_query_authTestEntity");
        public static TestPermissions QueryAuthorizedField => new("test_authTestEntity_authorizedField");
        public static TestPermissions Create => new("test_mutation_createAuthTestEntity");
    }
    public class TestAuthQuery : QueryExtension<TestAuthQuery>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<TestAuthQuery> descriptor)
        {
            base.Configure(descriptor);
        }

        public AuthTestEntity AuthTestEntity(string id) => throw new NotImplementedException();
    }

    public class TestAuthMutation : MutationExtension<TestAuthMutation>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<TestAuthMutation> descriptor)
        {
            base.Configure(descriptor);
        }

        public bool CreateAuthTestEntity(string name) => throw new NotImplementedException();
    }
    public class AuthTestEntity : Entity<AuthTestEntity>
    {
        public string AuthorizedField => "1";
    }
}

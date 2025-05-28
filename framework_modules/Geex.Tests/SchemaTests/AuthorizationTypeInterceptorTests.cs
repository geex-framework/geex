using Geex.Gql;
using Geex.Gql.Types;
using Geex.Storage;
using Geex.Tests.FeatureTests;
using Geex.Tests.SchemaTests.TestEntities;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Geex.Tests.SchemaTests
{
    public class AuthTestQuery : QueryExtension<AuthTestQuery>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<AuthTestQuery> descriptor)
        {
            base.Configure(descriptor);
        }

        public AuthTestEntity AuthTestQueryField(string id) => throw new NotImplementedException();
    }
    public class AuthTestMutation : MutationExtension<AuthTestMutation>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<AuthTestMutation> descriptor)
        {
            base.Configure(descriptor);
        }

        public bool AuthTestMutationField(string name) => throw new NotImplementedException();
    }
    [Collection(nameof(TestsCollection))]
    public class AuthorizationTypeInterceptorTests
    {
        private readonly TestApplicationFactory _factory;

        public AuthorizationTypeInterceptorTests(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ImplicitAuthorizationShouldBeApplied()
        {
            // Arrange
            var service = _factory.Services;
            var schema = service.GetService<ISchema>();

            // Get the schema type for our test aggregate entity
            var aggregateType = schema.GetType<ObjectType>(nameof(AuthTestEntity));
            aggregateType.ShouldNotBeNull();
            var entityName = typeof(AuthTestEntity).Name.ToCamelCase();

            var queryType = schema.GetType<ObjectType>(nameof(Query));
            TestPermissions.AuthTestQueryField.Value.ShouldEndWith(queryType.Fields.First(x => x.Name == nameof(AuthTestQuery.AuthTestQueryField).ToCamelCase()).Directives.First(x => x.Type.Name == "authorize").AsValue<AuthorizeDirective>().Policy);

            var mutationType = schema.GetType<ObjectType>(nameof(Mutation));
            TestPermissions.AuthTestMutationField.Value.ShouldEndWith(mutationType.Fields.First(x => x.Name == nameof(AuthTestMutation.AuthTestMutationField).ToCamelCase()).Directives.First(x => x.Type.Name == "authorize").AsValue<AuthorizeDirective>().Policy);

            // Check if permission-based field authorization is applied to fields
            var authFields = aggregateType.Fields.Where(x => x.Directives.Any(y => y.Type.Name == "authorize")).ToList();
            authFields.ShouldNotBeEmpty();
            var authTestField = authFields.FirstOrDefault(x => x.Name == nameof(AuthTestEntity.AuthorizedField).ToCamelCase());
            var authorizeDirective = authTestField.Directives.First(x => x.Type.Name == "authorize").AsValue<AuthorizeDirective>();
            TestPermissions.AuthTestEntityAuthorizedField.Value.ShouldEndWith(authorizeDirective.Policy);


        }

        [Fact]
        public async Task ExtensionTypeAuthorizationShouldBeApplied()
        {
            // Arrange
            var service = _factory.Services;
            var schema = service.GetService<ISchema>();

            // Check mutation type
            var mutationType = schema.GetType<ObjectType>(nameof(Mutation));
            if (mutationType != null)
            {
                var operationPrefix = typeof(AuthTestMutation).DomainName().ToCamelCase() + "_mutation";

                foreach (var field in mutationType.Fields)
                {
                    var policy = $"{operationPrefix}_{field.Name.RemovePreFix("Get").ToCamelCase()}";
                    if (AppPermission.List.Any(x => x.Value == policy))
                    {
                        field.Directives.Any(d => d.Type.Name == "authorize").ShouldBeTrue();
                    }
                }
            }

            // Check query type
            var queryType = schema.GetType<ObjectType>(nameof(Query));
            if (queryType != null)
            {
                var operationPrefix = typeof(AuthTestQuery).DomainName().ToCamelCase() + "_query";

                foreach (var field in queryType.Fields)
                {
                    var policy = $"{operationPrefix}_{field.Name.RemovePreFix("Get").ToCamelCase()}";
                    if (AppPermission.List.Any(x => x.Value == policy))
                    {
                        field.Directives.Any(d => d.Type.Name == "authorize").ShouldBeTrue();
                    }
                }
            }
        }

        [Fact]
        public async Task SpecialFieldsShouldNotBeAuthorized()
        {
            // Arrange
            var service = _factory.Services;
            var schema = service.GetService<ISchema>();

            // Get test aggregate type
            var aggregateType = schema.GetType<ObjectType>(nameof(AuthTestEntity));
            aggregateType.ShouldNotBeNull();

            // Common fields that should not get automatic authorization
            var commonFields = new[] { "id", "createdOn", "updatedOn" };

            foreach (var commonField in commonFields)
            {
                if (aggregateType.Fields.TryGetField(commonField, out var field))
                {
                    // Common fields should typically not have authorize directives
                    field.Directives.Any(d => d.Type.Name == "authorize").ShouldBeFalse();
                }
            }
        }
    }
}

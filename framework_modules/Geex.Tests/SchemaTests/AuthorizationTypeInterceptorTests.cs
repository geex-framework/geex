using Geex.Gql;
using Geex.Gql.Types;
using Geex.Storage;
using Geex.Tests.TestEntities;

using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Geex.Tests.SchemaTests
{
    public class AuthorizationTypeInterceptorTests : IClassFixture<GeexWebApplicationFactory>
    {
        private readonly GeexWebApplicationFactory _factory;

        public AuthorizationTypeInterceptorTests(GeexWebApplicationFactory factory)
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
            TestPermissions.Query.Value.ShouldEndWith(queryType.Fields.First(x => x.Name == entityName).Directives.First(x => x.Type.Name == "authorize").AsValue<AuthorizeDirective>().Policy);

            var mutationType = schema.GetType<ObjectType>(nameof(Mutation));
            TestPermissions.Create.Value.ShouldEndWith(mutationType.Fields.First(x => x.Name == nameof(TestAuthMutation.CreateAuthTestEntity).ToCamelCase()).Directives.First(x => x.Type.Name == "authorize").AsValue<AuthorizeDirective>().Policy);

            // Check if permission-based field authorization is applied to fields
            var authFields = aggregateType.Fields.Where(x => x.Directives.Any(y => y.Type.Name == "authorize")).ToList();
            authFields.ShouldNotBeEmpty();
            var authTestField = authFields.FirstOrDefault(x => x.Name == nameof(AuthTestEntity.AuthorizedField).ToCamelCase());
            var authorizeDirective = authTestField.Directives.First(x => x.Type.Name == "authorize").AsValue<AuthorizeDirective>();
            TestPermissions.QueryAuthorizedField.Value.ShouldEndWith(authorizeDirective.Policy);


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
                var operationPrefix = typeof(TestAuthMutation).DomainName().ToCamelCase() + "_mutation";

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
                var operationPrefix = typeof(TestAuthQuery).DomainName().ToCamelCase() + "_query";

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

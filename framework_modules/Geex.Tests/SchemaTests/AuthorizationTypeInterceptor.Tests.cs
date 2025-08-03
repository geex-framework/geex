using Geex.Extensions.Authorization.Core.Utils;
using Geex.Gql.Types;
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
    public class AuthorizationTypeInterceptorTests : TestsBase
    {
        public AuthorizationTypeInterceptorTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task ImplicitAuthorizationShouldBeApplied()
        {
            // Arrange

            var schema = ScopedService.GetService<ISchema>();

            // Get the schema type for our test aggregate entity
            var aggregateType = schema.GetType<ObjectType>(nameof(AuthTestEntity));
            aggregateType.ShouldNotBeNull();
            var entityName = typeof(AuthTestEntity).Name.ToCamelCase();

            var queryType = schema.GetType<ObjectType>(nameof(Query));
            var authQueryField = queryType.Fields.First(x => x.Name == nameof(AuthTestQuery.AuthTestQueryField).ToCamelCase());
            authQueryField.Directives.ContainsDirective<AuthorizeDirective>().ShouldBeTrue();

            var mutationType = schema.GetType<ObjectType>(nameof(Mutation));
            var authMutationField = mutationType.Fields.First(x => x.Name == nameof(AuthTestMutation.AuthTestMutationField).ToCamelCase());
            authMutationField.Directives.ContainsDirective<AuthorizeDirective>().ShouldBeTrue();

            // Check if permission-based field authorization is applied to fields
            var authFields = aggregateType.Fields.Where(x => x.Middleware.Method.DeclaringType.FullName.Contains(nameof(AuthorizationTypeInterceptor))).ToList();
            authFields.ShouldNotBeEmpty();
            var authTestField = authFields.FirstOrDefault(x => x.Name == nameof(AuthTestEntity.AuthorizedField).ToCamelCase());
            authTestField.Directives.ContainsDirective<AuthorizeDirective>().ShouldBeTrue();
        }

        [Fact]
        public async Task ExtensionTypeAuthorizationShouldBeApplied()
        {
            // Arrange

            var schema = ScopedService.GetService<ISchema>();

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

            var schema = ScopedService.GetService<ISchema>();

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

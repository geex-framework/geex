using Geex.Gql;
using Geex.Gql.Types;
using Geex.Storage;
using Geex.Tests.TestEntities;

using HotChocolate;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Geex.Tests.SchemaTests
{
    public class AuthorizationTypeInterceptorTests : IClassFixture<GeexWebApplicationFactory>
    {
        public class TestAuthQuery : QueryExtension<TestAuthQuery>
        {
            /// <inheritdoc />
            protected override void Configure(IObjectTypeDescriptor<TestAuthQuery> descriptor)
            {
                base.Configure(descriptor);
            }

            public TestEntity GetTestEntity(string id) => throw new NotImplementedException();
        }

        public class TestAuthMutation : MutationExtension<TestAuthMutation>
        {
            /// <inheritdoc />
            protected override void Configure(IObjectTypeDescriptor<TestAuthMutation> descriptor)
            {
                base.Configure(descriptor);
            }

            public bool CreateTestEntity(TestEntity entity) => throw new NotImplementedException();
        }

        // Test aggregate entity
        public class TestAggregateEntity : Entity<TestAggregateEntity>
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public DateTime CreatedOn { get; set; }
            public string TestField { get; set; }
        }

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
            var aggregateType = schema.GetType<ObjectType>(nameof(TestAggregateEntity));
            aggregateType.ShouldNotBeNull();

            // Check if permission-based field authorization is applied to fields
            var moduleName = typeof(TestAggregateEntity).DomainName();
            var entityName = typeof(TestAggregateEntity).Name.ToCamelCase();
            var prefix = $"{moduleName}_query_{entityName}";

            // Check if directives are applied for fields with matching permissions
            foreach (var field in aggregateType.Fields)
            {
                var policy = $"{prefix}_{field.Name.ToCamelCase()}";

                // If a permission with this name exists, it should have an authorize directive
                if (AppPermission.List.Any(x => x.Value == policy))
                {
                    field.Directives.Any(d => d.Type.Name == "authorize").ShouldBeTrue();
                }
            }
        }

        [Fact]
        public async Task ExtensionTypeAuthorizationShouldBeApplied()
        {
            // Arrange
            var service = _factory.Services;
            var schema = service.GetService<ISchema>();

            // Check mutation type
            var mutationType = schema.GetType<ObjectType>("TestAuthMutation");
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
            var queryType = schema.GetType<ObjectType>("TestAuthQuery");
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
            var aggregateType = schema.GetType<ObjectType>(nameof(TestAggregateEntity));
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

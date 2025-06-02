using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Gql;
using Geex.Gql.Types;
using Geex.Tests.FeatureTests;
using Geex.Tests.SchemaTests.TestEntities;

using HotChocolate;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Geex.Tests.SchemaTests
{
    public class CoreTestMutation : MutationExtension<CoreTestMutation>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<CoreTestMutation> descriptor)
        {
            base.Configure(descriptor);
        }        public TestEntity CoreTestDirectMutation(string arg1) => throw new NotImplementedException();
        public Lazy<TestEntity> CoreTestLazyMutation(string arg1) => throw new NotImplementedException();
        public IQueryable<TestEntity> CoreTestIQueryableMutation(string arg1) => throw new NotImplementedException();
    }

    [Collection(nameof(TestsCollection))]
    public class CoreInterceptorsTests : TestsBase
    {
        public class TestOneOfConfig
        {
            public string CommonField { get; set; }
        }

        public class TestOneOfType1
        {
            public string Type1Field { get; set; }
        }

        public class TestOneOfType2
        {
            public string Type2Field { get; set; }
        }        public CoreInterceptorsTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task LazyPropertiesShouldBeResolved()
        {
            // Arrange

            var schema = ScopedService.GetService<ISchema>();

            // Act & Assert
            schema.MutationType.Fields.TryGetField(nameof(CoreTestMutation.CoreTestLazyMutation).ToCamelCase(), out var lazyField).ShouldBeTrue();
            lazyField.ShouldNotBeNull();
            lazyField.Type.NamedType().Name.ShouldBe($"LazyOf{nameof(TestEntity)}");

            // 确认Resolver已被配置
            lazyField.Resolver.ShouldNotBeNull();
        }

        [Fact]
        public async Task QueryablePropertiesShouldBeResolved()
        {
            // Arrange

            var schema = ScopedService.GetService<ISchema>();

            // Act & Assert
            schema.MutationType.Fields.TryGetField(nameof(CoreTestMutation.CoreTestIQueryableMutation).ToCamelCase(), out var queryableField).ShouldBeTrue();
            queryableField.ShouldNotBeNull();
            (queryableField.Type as NonNullType).Type.ShouldBeOfType<ListType>();
            (((queryableField.Type as NonNullType).Type as ListType).ElementType as NonNullType).Type.TypeName().ShouldBe(nameof(TestEntity));

            // 确认Resolver已被配置
            queryableField.Resolver.ShouldNotBeNull();
        }

        [Fact]
        public async Task SpecialFieldsShouldBeIgnored()
        {
            // Arrange

            var schema = ScopedService.GetService<ISchema>();

            // Act & Assert
            var queryType = schema.QueryType;
            queryType.Fields.TryGetField("name", out var nameField).ShouldBeFalse();
            queryType.Fields.TryGetField("kind", out var kindField).ShouldBeFalse();
            queryType.Fields.TryGetField("scope", out var scopeField).ShouldBeFalse();
            queryType.Fields.TryGetField("description", out var descField).ShouldBeFalse();
            queryType.Fields.TryGetField("contextData", out var contextField).ShouldBeFalse();

            var entityType = schema.Types.FirstOrDefault(x => x.TypeName() == nameof(TestEntity));
            entityType.NamedType().As<ObjectType>().Fields.TryGetField("validate", out var entityNameField).ShouldBeFalse();
        }

        [Fact(Skip = "todo")]
        public async Task OneOfConfigsShouldWork()
        {
            // Arrange

            var schema = ScopedService.GetService<ISchema>();

            // Act
            var inputTypes = schema.Types.OfType<InputObjectType>().ToList();
            var testOneOfConfigType = inputTypes.FirstOrDefault(x => x.Name == nameof(TestOneOfConfig).ToCamelCase());

            // Assert
            testOneOfConfigType.ShouldNotBeNull();
            testOneOfConfigType.Fields.TryGetField("commonField", out var commonField).ShouldBeTrue();
            testOneOfConfigType.Fields.TryGetField("testOneOfType1", out var type1Field).ShouldBeTrue();
            testOneOfConfigType.Fields.TryGetField("testOneOfType2", out var type2Field).ShouldBeTrue();

            // 检查oneOf指令是否已添加
            testOneOfConfigType.Directives.Any(d => d.Type.Name == "oneOf").ShouldBeTrue();
        }
    }
}

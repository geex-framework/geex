using Geex.Gql;
using Geex.Gql.Types;
using Geex.Tests.SchemaTests.TestEntities;
using HotChocolate;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Geex.Tests.SchemaTests
{
    /// <summary>
    /// 测试删除 Mutation 扩展
    /// </summary>
    public class DeleteEntityMutation : MutationExtension<DeleteEntityMutation>, IHasDeleteMutation<TestEntity>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<DeleteEntityMutation> descriptor)
        {
            base.Configure(descriptor);
            // 使用扩展方法配置删除字段
            //descriptor.Field(x => x.Delete(default));
        }

        public TestEntity TestEntityTestMutationField(string arg1) => throw new NotImplementedException();
    }

    [Collection(nameof(TestsCollection))]
    public class DeleteMutationTypeInterceptorTests : TestsBase
    {
        public DeleteMutationTypeInterceptorTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task DeleteMutationMethodsShouldBeAdded()
        {
            // Arrange
            var schema = ScopedService.GetService<ISchema>();

            // Act & Assert
            // Check if the mutation type has the delete method added
            var entityName = nameof(TestEntity);
            schema.MutationType.Fields.TryGetField($"delete{entityName}", out var deleteField).ShouldBeTrue();

            // Verify field arguments
            deleteField.Arguments.Count.ShouldBe(1);
            deleteField.Arguments.Any(a => a.Name == "ids").ShouldBeTrue();

            // Verify return type is Boolean
            deleteField.Type.ToString().ShouldContain("Boolean");
        }

        [Fact]
        public async Task DeleteMutationExtensionMethodShouldWork()
        {
            // Arrange
            var schema = ScopedService.GetService<ISchema>();

            // Act & Assert
            // Verify that the extension method created the field properly
            var entityName = nameof(TestEntity);
            schema.MutationType.Fields.TryGetField($"delete{entityName}", out var deleteField).ShouldBeTrue();

            // The field should be created by the extension method
            deleteField.ShouldNotBeNull();
            deleteField.Name.ShouldBe($"delete{entityName}");
        }
    }
}

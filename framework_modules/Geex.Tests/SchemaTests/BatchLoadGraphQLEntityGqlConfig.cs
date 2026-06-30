using Geex.Tests.SchemaTests.TestEntities;

using HotChocolate.Types;

namespace Geex.Tests.SchemaTests
{
    public class BatchLoadGraphQLEntityGqlConfig : GqlConfig.Object<BatchLoadGraphQLEntity>
    {
        protected override void Configure(IObjectTypeDescriptor<BatchLoadGraphQLEntity> descriptor)
        {
            descriptor.Implements<InterfaceType<IBatchLoadGraphQLEntity>>();
            descriptor.Field(x => x.Children).Name("childNodes");
        }
    }
}

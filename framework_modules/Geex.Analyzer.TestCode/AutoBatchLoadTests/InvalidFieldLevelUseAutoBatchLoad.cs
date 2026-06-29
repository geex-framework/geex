using Geex.Gql.Types;

using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests
{
    public static class InvalidFieldLevelExtensions
    {
        public static IObjectFieldDescriptor UseAutoBatchLoad(this IObjectFieldDescriptor descriptor, bool enabled) =>
            descriptor;
    }

    public class InvalidFieldLevelUseAutoBatchLoadQuery : QueryExtension<InvalidFieldLevelUseAutoBatchLoadQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<InvalidFieldLevelUseAutoBatchLoadQuery> descriptor)
        {
            descriptor.Field("batchLoadEntities")
                .Type<StringType>()
                .Resolve(_ => "test")
                .UseAutoBatchLoad(false);

            base.Configure(descriptor);
        }
    }
}

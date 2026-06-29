using Geex.Gql.AutoBatchLoad;
using Geex.Gql.Types;

using HotChocolate.Types;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests
{
    public class ValidOperationLevelUseAutoBatchLoadQuery : QueryExtension<ValidOperationLevelUseAutoBatchLoadQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<ValidOperationLevelUseAutoBatchLoadQuery> descriptor)
        {
            descriptor.UseAutoBatchLoad(false);
            base.Configure(descriptor);
        }
    }
}

using Geex;
using Geex.Gql.AutoBatchLoad;

using HotChocolate.Types;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests
{
    public class InvalidEntityTypeTestEntity
    {
        public string Name { get; set; } = default!;
    }

    public class InvalidEntityTypeLevelUseAutoBatchLoadConfig : GqlConfig.Object<InvalidEntityTypeTestEntity>
    {
        protected override void Configure(IObjectTypeDescriptor<InvalidEntityTypeTestEntity> descriptor)
        {
            descriptor.UseAutoBatchLoad(true);
        }
    }
}

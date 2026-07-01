using Geex;
using Geex.Gql.AutoBatchLoad;

using HotChocolate.Types;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests
{
    public class InvalidEntityFieldTestEntity
    {
        public string Name { get; set; } = default!;
    }

    public class InvalidEntityFieldLevelUseAutoBatchLoadConfig : GqlConfig.Object<InvalidEntityFieldTestEntity>
    {
        protected override void Configure(IObjectTypeDescriptor<InvalidEntityFieldTestEntity> descriptor)
        {
            descriptor.Field(x => x.Name).UseAutoBatchLoad(true);
        }
    }
}

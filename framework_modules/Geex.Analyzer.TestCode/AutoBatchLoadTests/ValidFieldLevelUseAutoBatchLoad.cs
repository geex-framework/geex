using System.Linq;

using Geex.Gql.AutoBatchLoad;
using Geex.Gql.Types;

using HotChocolate.Types;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests
{
    public class ValidFieldLevelUseAutoBatchLoadQuery : QueryExtension<ValidFieldLevelUseAutoBatchLoadQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<ValidFieldLevelUseAutoBatchLoadQuery> descriptor)
        {
            descriptor.Field(x => x.BatchLoadEntities()).UseAutoBatchLoad(true);
            base.Configure(descriptor);
        }

        public IQueryable<ValidFieldLevelEntity> BatchLoadEntities() =>
            Enumerable.Empty<ValidFieldLevelEntity>().AsQueryable();
    }

    public class ValidFieldLevelEntity
    {
        public string Id { get; set; } = default!;
    }
}

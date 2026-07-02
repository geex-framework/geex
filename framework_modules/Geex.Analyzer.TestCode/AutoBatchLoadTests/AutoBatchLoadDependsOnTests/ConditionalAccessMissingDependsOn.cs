using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.AutoBatchLoadDependsOnTests
{
    public class AutoBatchLoadDependsOnConditionalAccessMissingTestEntity : Entity<AutoBatchLoadDependsOnConditionalAccessMissingTestEntity>
    {
        public AutoBatchLoadDependsOnConditionalAccessMissingTestEntity()
        {
            ConfigLazyQuery(
                x => x.Lines,
                _ => true,
                _ => _ => true);
        }

        public int LineCount => Lines?.Count() ?? 0;

        public IQueryable<AutoBatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
    }
}

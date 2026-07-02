using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.BatchLoadDependsOnTests
{
    public class BatchLoadDependsOnConditionalAccessMissingTestEntity : Entity<BatchLoadDependsOnConditionalAccessMissingTestEntity>
    {
        public BatchLoadDependsOnConditionalAccessMissingTestEntity()
        {
            ConfigLazyQuery(
                x => x.Lines,
                _ => true,
                _ => _ => true);
        }

        public int LineCount => Lines?.Count() ?? 0;

        public IQueryable<BatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
    }
}

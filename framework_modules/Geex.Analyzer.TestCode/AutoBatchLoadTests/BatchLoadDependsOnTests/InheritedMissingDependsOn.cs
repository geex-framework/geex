using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.BatchLoadDependsOnTests
{
    public class BatchLoadDependsOnBaseTestEntity : Entity<BatchLoadDependsOnBaseTestEntity>
    {
        public BatchLoadDependsOnBaseTestEntity()
        {
            ConfigLazyQuery(
                x => x.Lines,
                _ => true,
                _ => _ => true);
        }

        public IQueryable<BatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
    }

    public class BatchLoadDependsOnDerivedMissingTestEntity : BatchLoadDependsOnBaseTestEntity
    {
        public decimal TotalAmount => Lines.Sum(x => x.Amount);
    }
}

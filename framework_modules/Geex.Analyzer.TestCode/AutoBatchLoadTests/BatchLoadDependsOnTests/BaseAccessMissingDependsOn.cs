using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.BatchLoadDependsOnTests
{
    public class BatchLoadDependsOnBaseAccessBaseTestEntity : Entity<BatchLoadDependsOnBaseAccessBaseTestEntity>
    {
        public BatchLoadDependsOnBaseAccessBaseTestEntity()
        {
            ConfigLazyQuery(
                x => x.Lines,
                _ => true,
                _ => _ => true);
        }

        public IQueryable<BatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
    }

    public class BatchLoadDependsOnBaseAccessMissingTestEntity : BatchLoadDependsOnBaseAccessBaseTestEntity
    {
        public decimal TotalAmount => base.Lines.Sum(x => x.Amount);
    }
}

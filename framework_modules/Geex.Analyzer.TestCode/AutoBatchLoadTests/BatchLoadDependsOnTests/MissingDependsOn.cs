using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.BatchLoadDependsOnTests
{
    public class BatchLoadDependsOnTestLineEntity : Entity<BatchLoadDependsOnTestLineEntity>
    {
        public decimal Amount { get; set; }
    }

    public class BatchLoadDependsOnMissingTestEntity : Entity<BatchLoadDependsOnMissingTestEntity>
    {
        public BatchLoadDependsOnMissingTestEntity()
        {
            ConfigLazyQuery(
                x => x.Lines,
                _ => true,
                _ => _ => true);
        }

        public decimal TotalAmount => Lines.Sum(x => x.Amount);

        public IQueryable<BatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
    }
}

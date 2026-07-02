using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.BatchLoadDependsOnTests
{
    public class BatchLoadDependsOnPrivateLazyNavMissingTestEntity : Entity<BatchLoadDependsOnPrivateLazyNavMissingTestEntity>
    {
        public BatchLoadDependsOnPrivateLazyNavMissingTestEntity()
        {
            ConfigLazyQuery(
                x => x.Lines,
                _ => true,
                _ => _ => true);
        }

        public decimal TotalAmount => Lines.Sum(x => x.Amount);

        private IQueryable<BatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
    }
}

using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.AutoBatchLoadDependsOnTests
{
    public class AutoBatchLoadDependsOnPrivateLazyNavMissingTestEntity : Entity<AutoBatchLoadDependsOnPrivateLazyNavMissingTestEntity>
    {
        public AutoBatchLoadDependsOnPrivateLazyNavMissingTestEntity()
        {
            ConfigLazyQuery(
                x => x.Lines,
                _ => true,
                _ => _ => true);
        }

        public decimal TotalAmount => Lines.Sum(x => x.Amount);

        private IQueryable<AutoBatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
    }
}

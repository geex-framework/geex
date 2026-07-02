using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.AutoBatchLoadDependsOnTests
{
    public class AutoBatchLoadDependsOnTestLineEntity : Entity<AutoBatchLoadDependsOnTestLineEntity>
    {
        public decimal Amount { get; set; }
    }

    public class AutoBatchLoadDependsOnMissingTestEntity : Entity<AutoBatchLoadDependsOnMissingTestEntity>
    {
        public AutoBatchLoadDependsOnMissingTestEntity()
        {
            ConfigLazyQuery(
                x => x.Lines,
                _ => true,
                _ => _ => true);
        }

        public decimal TotalAmount => Lines.Sum(x => x.Amount);

        public IQueryable<AutoBatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
    }
}

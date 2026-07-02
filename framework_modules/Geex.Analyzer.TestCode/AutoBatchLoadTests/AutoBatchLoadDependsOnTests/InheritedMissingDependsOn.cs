using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.AutoBatchLoadDependsOnTests
{
    public class AutoBatchLoadDependsOnBaseTestEntity : Entity<AutoBatchLoadDependsOnBaseTestEntity>
    {
        public AutoBatchLoadDependsOnBaseTestEntity()
        {
            ConfigLazyQuery(
                x => x.Lines,
                _ => true,
                _ => _ => true);
        }

        public IQueryable<AutoBatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
    }

    public class AutoBatchLoadDependsOnDerivedMissingTestEntity : AutoBatchLoadDependsOnBaseTestEntity
    {
        public decimal TotalAmount => Lines.Sum(x => x.Amount);
    }
}

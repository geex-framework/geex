using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.AutoBatchLoadDependsOnTests
{
    public class AutoBatchLoadDependsOnBaseAccessBaseTestEntity : Entity<AutoBatchLoadDependsOnBaseAccessBaseTestEntity>
    {
        public AutoBatchLoadDependsOnBaseAccessBaseTestEntity()
        {
            ConfigLazyQuery(
                x => x.Lines,
                _ => true,
                _ => _ => true);
        }

        public IQueryable<AutoBatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
    }

    public class AutoBatchLoadDependsOnBaseAccessMissingTestEntity : AutoBatchLoadDependsOnBaseAccessBaseTestEntity
    {
        public decimal TotalAmount => base.Lines.Sum(x => x.Amount);
    }
}

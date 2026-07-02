using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.AutoBatchLoadDependsOnTests
{
    public class AutoBatchLoadDependsOnPrivateMethodMissingTestEntity : Entity<AutoBatchLoadDependsOnPrivateMethodMissingTestEntity>
    {
        public AutoBatchLoadDependsOnPrivateMethodMissingTestEntity()
        {
            ConfigLazyQuery(
                x => x.Lines,
                _ => true,
                _ => _ => true);
        }

        public decimal TotalAmount => CalculateTotal();

        private decimal CalculateTotal() => Lines.Sum(x => x.Amount);

        public IQueryable<AutoBatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
    }
}

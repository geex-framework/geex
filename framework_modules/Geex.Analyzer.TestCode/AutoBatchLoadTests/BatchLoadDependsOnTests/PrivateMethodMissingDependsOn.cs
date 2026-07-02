using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.BatchLoadDependsOnTests
{
    public class BatchLoadDependsOnPrivateMethodMissingTestEntity : Entity<BatchLoadDependsOnPrivateMethodMissingTestEntity>
    {
        public BatchLoadDependsOnPrivateMethodMissingTestEntity()
        {
            ConfigLazyQuery(
                x => x.Lines,
                _ => true,
                _ => _ => true);
        }

        public decimal TotalAmount => CalculateTotal();

        private decimal CalculateTotal() => Lines.Sum(x => x.Amount);

        public IQueryable<BatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
    }
}

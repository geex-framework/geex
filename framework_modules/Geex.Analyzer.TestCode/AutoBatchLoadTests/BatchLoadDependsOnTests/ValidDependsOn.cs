using System.Linq;

using Geex.Gql.Attributes;
using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.BatchLoadDependsOnTests
{
    public class BatchLoadDependsOnValidTestEntity : Entity<BatchLoadDependsOnValidTestEntity>
    {
        public BatchLoadDependsOnValidTestEntity()
        {
            ConfigLazyQuery(
                x => x.Lines,
                _ => true,
                _ => _ => true);
        }

        [BatchLoadDependsOn(nameof(Lines))]
        public decimal TotalAmount => Lines.Sum(x => x.Amount);

        public IQueryable<BatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
    }
}

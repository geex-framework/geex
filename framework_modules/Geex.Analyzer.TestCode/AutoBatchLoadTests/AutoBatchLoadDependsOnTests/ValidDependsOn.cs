using System.Linq;

using Geex.Gql.Attributes;
using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.AutoBatchLoadDependsOnTests
{
    public class AutoBatchLoadDependsOnValidTestEntity : Entity<AutoBatchLoadDependsOnValidTestEntity>
    {
        public AutoBatchLoadDependsOnValidTestEntity()
        {
            ConfigLazyQuery(
                x => x.Lines,
                _ => true,
                _ => _ => true);
        }

        [AutoBatchLoadDependsOn(nameof(Lines))]
        public decimal TotalAmount => Lines.Sum(x => x.Amount);

        public IQueryable<AutoBatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
    }
}

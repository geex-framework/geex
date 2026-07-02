using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.BatchLoadDependsOnTests
{
    public class BatchLoadDependsOnMissingMultipleTestEntity : Entity<BatchLoadDependsOnMissingMultipleTestEntity>
    {
        public BatchLoadDependsOnMissingMultipleTestEntity()
        {
            ConfigLazyQuery(
                x => x.Lines,
                _ => true,
                _ => _ => true);
            ConfigLazyQuery(
                x => x.ArchivedLines,
                _ => true,
                _ => _ => true);
        }

        public int Summary => Lines.Count() + ArchivedLines.Count();

        public IQueryable<BatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
        public IQueryable<BatchLoadDependsOnTestLineEntity> ArchivedLines => LazyQuery(() => ArchivedLines);
    }
}

using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Analyzer.TestCode.AutoBatchLoadTests.AutoBatchLoadDependsOnTests
{
    public class AutoBatchLoadDependsOnMissingMultipleTestEntity : Entity<AutoBatchLoadDependsOnMissingMultipleTestEntity>
    {
        public AutoBatchLoadDependsOnMissingMultipleTestEntity()
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

        public IQueryable<AutoBatchLoadDependsOnTestLineEntity> Lines => LazyQuery(() => Lines);
        public IQueryable<AutoBatchLoadDependsOnTestLineEntity> ArchivedLines => LazyQuery(() => ArchivedLines);
    }
}

using MongoDB.Entities.Utilities;

namespace Geex.Gql.AutoBatchLoad
{
    public static class SelectionBatchLoadApplier
    {
        public static void ApplyOverlay(BatchLoadConfig target, BatchLoadConfig selectionTree) =>
            target.ApplySelectionOverlay(selectionTree);
    }
}

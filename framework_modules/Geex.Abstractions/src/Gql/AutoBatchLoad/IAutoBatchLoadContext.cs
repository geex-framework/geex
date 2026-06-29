using System.Collections.Generic;

using MongoDB.Entities.Utilities;

namespace Geex.Gql.AutoBatchLoad
{
    public interface IAutoBatchLoadContext
    {
        BatchLoadConfig? CurrentOverlay { get; }

        void PushOverlay(BatchLoadConfig config);

        void PopOverlay();
    }

    internal sealed class AutoBatchLoadContext : IAutoBatchLoadContext
    {
        private readonly Stack<BatchLoadConfig> _overlayStack = new();

        public BatchLoadConfig? CurrentOverlay => _overlayStack.Count > 0 ? _overlayStack.Peek() : null;

        public void PushOverlay(BatchLoadConfig config) => _overlayStack.Push(config);

        public void PopOverlay()
        {
            if (_overlayStack.Count > 0)
            {
                _overlayStack.Pop();
            }
        }
    }
}

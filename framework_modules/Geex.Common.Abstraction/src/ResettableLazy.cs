using System.Threading;

// ReSharper disable once CheckNamespace
namespace System
{
    public class ResettableLazy<T>
    {
        private Lazy<T> _lazy;

        public bool IsValueCreated => _lazy.IsValueCreated;

        public T Value => _lazy.Value;

        public LazyThreadSafetyMode LazyThreadSafetyMode { get; }

        private readonly Func<T> _valueFactory;

        public ResettableLazy(Func<T> valueFactory, LazyThreadSafetyMode lazyThreadSafetyMode = LazyThreadSafetyMode.ExecutionAndPublication)
        {
            _valueFactory = valueFactory;
            LazyThreadSafetyMode = lazyThreadSafetyMode;
            _lazy = new Lazy<T>(_valueFactory, LazyThreadSafetyMode);
        }

        public ResettableLazy(Func<T> valueFactory, bool isThreadSafe)
            : this(valueFactory, isThreadSafe ? LazyThreadSafetyMode.ExecutionAndPublication : LazyThreadSafetyMode.None)
        {
        }

        public void Reset()
        {
            _lazy = new Lazy<T>(_valueFactory, LazyThreadSafetyMode);
        }
    }
}

namespace MongoDB.Entities.Interceptors
{
    public interface IIntercepted : IEntityBase
    {
    }
    public abstract class DataInterceptor<T> : IDataInterceptor<T> where T : IIntercepted
    {
        public abstract void Apply(T entity);

        /// <inheritdoc />
        public abstract InterceptorExecuteTiming InterceptAt { get; }
    }

    /// <summary>
    /// invoke when entity is saved(not commit)
    /// </summary>
    public interface IDataInterceptor<T> : IDataInterceptor where T : IIntercepted
    {
        void Apply(T entity);
        void IDataInterceptor.Apply(IEntityBase entity)
        {
            this.Apply((T)entity);
        }
    }
    /// <summary>
    /// invoke when entity is saved(not commit)
    /// </summary>
    public interface IDataInterceptor
    {
        void Apply(IEntityBase entity);
        InterceptorExecuteTiming InterceptAt { get; }
    }

    public enum InterceptorExecuteTiming
    {
        /// <summary>
        /// trigger when attach(attach multiple times will not take effect)
        /// </summary>
        Attach,
        /// <summary>
        /// trigger when save(will take effect every time)
        /// </summary>
        Save
    }
}
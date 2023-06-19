using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using MongoDB.Entities;
using MongoDB.Entities.Utilities;

namespace System.Linq
{
    internal class BatchLoadQueryable<TSource, TRelated> : IBatchLoadQueryable<TSource, TRelated> where TSource : IEntityBase
    {
        private readonly IQueryable<TSource> _sources;

        public BatchLoadQueryable(IQueryable<TSource> sources, PropertyInfo parentProp, bool rebindRoot)
        {
            this.ParentProp = parentProp;
            if (sources.Provider is not ICachedDbContextQueryProvider rootProvider)
            {
                throw new NotSupportedException("只支持CachedDbContextQueryable");
            }

            BatchLoadConfig config = rootProvider.BatchLoadConfig;
            this.Provider = rootProvider;
            config.SubBatchLoadConfigs.TryAdd(parentProp, new BatchLoadConfig());
            _sources = sources;
        }
        public BatchLoadQueryable(IQueryable<TSource> sources, PropertyInfo parentProp)
        {
            this.ParentProp = parentProp;
            ParentQuery = sources;

            if (sources.Provider is not ICachedDbContextQueryProvider rootProvider)
            {
                throw new NotSupportedException("只支持CachedDbContextQueryable");
            }

            BatchLoadConfig config = rootProvider.BatchLoadConfig;
            if (sources is IBatchLoadQueryable batchLoadQueryable)
            {
                var propQueue = new Queue<PropertyInfo>();
                var query = batchLoadQueryable;
                while (query != default)
                {
                    propQueue.Enqueue(query.ParentProp);
                    if (query.ParentQuery is IBatchLoadQueryable parentQuery)
                    {
                        query = parentQuery;
                    }
                    else
                    {
                        break;
                    }
                }
                propQueue = new Queue<PropertyInfo>(propQueue.Reverse());
                while (propQueue.TryDequeue(out var prop))
                {
                    config = config.SubBatchLoadConfigs[prop];
                }
            }
            this.Provider = rootProvider;
            config.SubBatchLoadConfigs.TryAdd(parentProp, new BatchLoadConfig());
            _sources = sources;
        }

        public IQueryable ParentQuery { get; set; }

        public PropertyInfo ParentProp { get; set; }

        /// <inheritdoc />
        public IEnumerator<TSource> GetEnumerator()
        {
            return _sources.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_sources).GetEnumerator();
        }

        /// <inheritdoc />
        public Type ElementType => _sources.ElementType;

        /// <inheritdoc />
        public Expression Expression => _sources.Expression;

        /// <inheritdoc />
        public IQueryProvider Provider { get; set; }
    }
}
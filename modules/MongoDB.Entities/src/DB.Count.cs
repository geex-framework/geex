using MongoDB.Driver;

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Entities.Interceptors;
using MongoDB.Entities.Utilities;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Gets a fast estimation of how many documents are in the collection using metadata.
        /// <para>HINT: The estimation may not be exactly accurate.</para>
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<long> CountEstimatedAsync<T>(CancellationToken cancellation = default) where T : IEntityBase
        {
            return Collection<T>().EstimatedDocumentCountAsync(null, cancellation);
        }

        /// <summary>
        /// Gets an accurate count of how many entities are matched for a given expression/filter
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="expression">A lambda expression for getting the count for a subset of the data</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        [Obsolete("使用Queryable Api")]
        public static async Task<long> CountAsync<T>(Expression<Func<T, bool>> expression, DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.Queryable<T>(dbContext: dbContext).Where(expression).LongCount();
        }

        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="filter">A filter definition</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static async Task<long> CountAsync<T>(FilterDefinition<T> filter, DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            if (dbContext?.DataFilters?.IsEmpty == false)
            {
                foreach (var (type, interceptor) in dbContext.DataFilters)
                {
                    if (type.IsAssignableFrom(typeof(T)))
                    {
                        filter = interceptor.PreFilter(filter);
                    }
                }
            }
            var count = await (
                dbContext?.Session == null
                    ? Collection<T>().CountDocumentsAsync(filter, null, cancellation)
                    : Collection<T>().CountDocumentsAsync(dbContext.Session, filter, null, cancellation));

            if (dbContext?.DataFilters?.IsEmpty == false)
            {
                foreach (var (type, interceptor) in dbContext.DataFilters)
                {
                    if (type.IsAssignableFrom(typeof(T)))
                    {
                        if (interceptor.PostFilterExpression != null)
                        {
                            throw new NotSupportedException("暂时不支持后置过滤的Count, 请查询后自行count");
                        }
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static async Task<long> CountAsync<T>(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter, DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            var filterInstance = filter(Builders<T>.Filter);
            if (dbContext?.DataFilters?.IsEmpty == false)
            {
                foreach (var (type, interceptor) in dbContext.DataFilters)
                {
                    if (type.IsAssignableFrom(typeof(T)))
                    {
                        filterInstance = interceptor.PreFilter(filterInstance);
                    }
                }
            }

            var count = await (
                 dbContext?.Session == null
                 ? Collection<T>().CountDocumentsAsync(filterInstance, null, cancellation)
                 : Collection<T>().CountDocumentsAsync(dbContext.Session, filterInstance, null, cancellation));

            if (dbContext?.DataFilters?.IsEmpty == false)
            {
                foreach (var (type, interceptor) in dbContext.DataFilters)
                {
                    if (type.IsAssignableFrom(typeof(T)))
                    {
                        if (interceptor.PostFilterExpression != null)
                        {
                            throw new NotSupportedException("暂时不支持后置过滤的Count, 请查询后自行count");
                        }
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<long> CountAsync<T>(DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            return CountAsync<T>(_ => true, dbContext, cancellation);
        }
    }
}

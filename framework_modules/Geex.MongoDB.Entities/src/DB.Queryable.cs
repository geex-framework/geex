using System;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.InnerQuery;
using MongoDB.Entities.Interceptors;
using MongoDB.Entities.Utilities;

namespace MongoDB.Entities
{

    public static partial class DB
    {
        /// <summary>
        /// Exposes the MongoDB collection for the given IEntity as an IQueryable in order to facilitate LINQ queries.
        /// </summary>
        /// <param name="options">The aggregate options</param>
        /// <param name="session">An optional session if used within a transaction</param>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static IQueryable<T> Queryable<T>(AggregateOptions options = null, DbContext dbContext = null) where T : IEntityBase
        {
            var collection = Collection<T>();
            var queryable = new MongoAggregationQueryable<T>(collection.CollectionNamespace.CollectionName);

            queryable.Expression = Expression.Constant(queryable);
            queryable.Provider = new MongoAggregationQueryProvider<T>(collection, dbContext?.Session, options)
            {
                Queryable = queryable,
                LoggingDelegate = (log) => dbContext?.ServiceProvider?.GetService<ILogger<DbContext>>()?.LogTrace(log),
            };
            var cachedQuery = new CachedDbContextQueryProvider<T>(queryable.Provider, dbContext).CreateQuery<T>(queryable.Expression);
            return cachedQuery;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

[assembly: InternalsVisibleTo("MongoDB.Entities.Tests")]
[assembly: InternalsVisibleTo("Benchmark")]
namespace MongoDB.Entities
{
    /// <summary>
    /// Extension methods for entities
    /// </summary>
    public static class Extensions
    {
        private class Holder<T>
        {
            public T Data { get; set; }
        }

        private static T Duplicate<T>(this T source)
        {
            return BsonSerializer.Deserialize<Holder<T>>(
                new Holder<T> { Data = source }.ToBson()
                ).Data;
        }

        internal static void ThrowIfUnsaved(this string entityID)
        {
            if (entityID == default)
                throw new InvalidOperationException("Please save the entity before performing this operation!");
        }

        internal static void ThrowIfUnsaved(this IEntityBase entity)
        {
            ThrowIfUnsaved(entity.Id);
        }

        /// <summary>
        /// Extension method for processing collections in batches with streaming (yield return)
        /// </summary>
        /// <typeparam name="T">The type of the objects inside the source collection</typeparam>
        /// <param name="collection">The source collection</param>
        /// <param name="batchSize">The size of each batch</param>
        public static IEnumerable<IEnumerable<T>> ToBatches<T>(this IEnumerable<T> collection, int batchSize = 100)
        {
            var batch = new List<T>(batchSize);

            foreach (T item in collection)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch.Clear();
                }
            }
            if (batch.Count > 0)
                yield return batch;
        }

        /// <summary>
        /// Gets the IMongoDatabase for the given entity type
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public static IMongoDatabase Database<T>(this T _) where T : IEntityBase => DB.Database<T>();

        /// <summary>
        /// Gets the name of the database this entity is attached to. Returns name of default database if not specifically attached.
        /// </summary>
        public static string DatabaseName<T>(this T _) where T : IEntityBase => DB.DatabaseName<T>();

        /// <summary>
        /// Gets the IMongoCollection for a given IEntity type.
        /// <para>TIP: Try never to use this unless really neccessary.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static IMongoCollection<T> Collection<T>(this T _) where T : IEntityBase => DB.Collection<T>();

        /// <summary>
        /// Gets the collection name for this entity
        /// </summary>
        public static string CollectionName<T>(this T _) where T : IEntityBase
        {
            return DB.CollectionName<T>();
        }

        /// <summary>
        /// Returns the full dotted path of a property for the given expression
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static string FullPath<T>(this Expression<Func<T, object>> expression)
        {
            return Prop.Path(expression);
        }

        ///// <summary>
        ///// An IQueryable collection of sibling Entities.
        ///// </summary>
        //public static IQueryable<T> Queryable<T>(this T _, AggregateOptions options = null) where T : IEntity
        //{
        //    return DB.Queryable<T>(options);
        //}

        /// <summary>
        /// An IAggregateFluent collection of sibling Entities.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        public static IAggregateFluent<T> Fluent<T>(this T _, AggregateOptions options = null) where T : IEntityBase
        {
            return DB.Fluent<T>(options, _.DbContext?.Session);
        }

        /// <summary>
        /// Adds a distinct aggregation stage to a fluent pipeline.
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        public static IAggregateFluent<T> Distinct<T>(this IAggregateFluent<T> aggregate) where T : IEntityBase
        {
            PipelineStageDefinition<T, T> groupStage = @"
                                                        {
                                                            $group: {
                                                                _id: '$_id',
                                                                doc: {
                                                                    $first: '$$ROOT'
                                                                }
                                                            }
                                                        }";

            PipelineStageDefinition<T, T> rootStage = @"
                                                        {
                                                            $replaceRoot: {
                                                                newRoot: '$doc'
                                                            }
                                                        }";

            return aggregate.AppendStage(groupStage).AppendStage(rootStage);
        }

        /// <summary>
        /// Appends a match stage to the pipeline with a filter expression
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="aggregate"></param>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        public static IAggregateFluent<T> Match<T>(this IAggregateFluent<T> aggregate, Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter) where T : IEntityBase
        {
            return aggregate.Match(filter(Builders<T>.Filter));
        }

        /// <summary>
        /// Appends a match stage to the pipeline with an aggregation expression (i.e. $expr)
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="aggregate"></param>
        /// <param name="expression">{ $gt: ['$Property1', '$Property2'] }</param>
        public static IAggregateFluent<T> MatchExpression<T>(this IAggregateFluent<T> aggregate, string expression) where T : IEntityBase
        {
            PipelineStageDefinition<T, T> stage = "{$match:{$expr:" + expression + "}}";

            return aggregate.AppendStage(stage);
        }

        /// <summary>
        /// Creates an unlinked duplicate of the original IEntity ready for embedding with a blank Id.
        /// </summary>
        public static T ToDocument<T>(this T entity) where T : IEntityBase
        {
            var res = entity.Duplicate();
            res.Id = res.GenerateNewId().ToString();
            return res;
        }

        /// <summary>
        /// Creates unlinked duplicates of the original Entities ready for embedding with blank Ids.
        /// </summary>
        public static T[] ToDocuments<T>(this T[] entities) where T : IEntityBase
        {
            var res = entities.Duplicate();
            foreach (var e in res)
                e.Id = e.GenerateNewId().ToString();
            return res;
        }

        /// <summary>
        ///Creates unlinked duplicates of the original Entities ready for embedding with blank Ids.
        /// </summary>
        public static IEnumerable<T> ToDocuments<T>(this IEnumerable<T> entities) where T : IEntityBase
        {
            var res = entities.Duplicate();
            foreach (var e in res)
                e.Id = e.GenerateNewId().ToString();
            return res;
        }

        /// <summary>
        /// Saves a complete entity replacing an existing entity or creating a new one if it does not exist.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is replaced.
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        internal static Task<WriteResult> SaveAsync<T>(this T entity, CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.SaveAsync(entity, entity.DbContext, cancellation);
        }

        /// <summary>
        /// Saves a batch of complete entities replacing existing ones or creating new ones if they do not exist.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is replaced.
        /// </summary>
        /// <param name="dbContext">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        internal static Task<BulkWriteResult<T>> SaveAsync<T>(this IEnumerable<T> entities, DbContext? dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.SaveAsync(entities, dbContext, cancellation);
        }

        /// <summary>
        /// Saves an entity partially with only the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        internal static Task<WriteResult> SaveOnlyAsync<T>(this T entity, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.SaveOnlyAsync(entity, members, entity.DbContext, cancellation);
        }

        /// <summary>
        /// Saves a batch of entities partially with only the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        internal static Task<BulkWriteResult<T>> SaveOnlyAsync<T>(this IEnumerable<T> entities, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntityBase
        {
            var enumerable = entities.ToList();
            return DB.SaveOnlyAsync(enumerable, members, enumerable.FirstOrDefault()?.DbContext, cancellation);
        }

        /// <summary>
        /// Saves an entity partially excluding the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        internal static Task<WriteResult> SaveExceptAsync<T>(this T entity, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.SaveExceptAsync(entity, members, entity.DbContext, cancellation);
        }

        /// <summary>
        /// Saves a batch of entities partially excluding the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        internal static Task<BulkWriteResult<T>> SaveExceptAsync<T>(this IEnumerable<T> entities, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntityBase
        {
            var enumerable = entities.ToList();
            return DB.SaveExceptAsync(enumerable, members, enumerable.FirstOrDefault()?.DbContext, cancellation);
        }

        /// <summary>
        /// Deletes multiple entities from the database
        /// </summary>
        internal static async Task<long> DeleteAsync<T>(this IEnumerable<T> entities,
            CancellationToken cancellationToken = default) where T : IEntityBase
        {
            var enumerable = entities.ToList();
            if (!enumerable.Any()) return 0;
            var dbContext = enumerable.FirstOrDefault()?.DbContext;
            if (dbContext != null)
            {
                dbContext.Detach(enumerable);
                return await dbContext.DeleteAsync(enumerable, cancellationToken);
            }
            return await DB.DeleteAsync<T>(enumerable.Select(x => x.Id), dbContext, cancellationToken);
        }

        /// <summary>
        /// Sort a list of objects by relevance to a given string using Levenshtein Distance
        /// </summary>
        /// <typeparam name="T">Any object type</typeparam>
        /// <param name="objects">The list of objects to sort</param>
        /// <param name="searchTerm">The term to measure relevance to</param>
        /// <param name="propertyToSortBy">x => x.PropertyName [the term will be matched against the value of this property]</param>
        /// <param name="maxDistance">The maximum levenstein distance to qualify an item for inclusion in the returned list</param>
        public static IEnumerable<T> SortByRelevance<T>(this IEnumerable<T> objects, string searchTerm, Func<T, string> propertyToSortBy, int? maxDistance = null)
        {
            var lev = new Levenshtein(searchTerm);

            var res = objects.Select(o => new
            {
                score = lev.DistanceFrom(propertyToSortBy(o)),
                obj = o
            });

            if (maxDistance.HasValue)
                res = res.Where(x => x.score <= maxDistance.Value);

            return res.OrderBy(x => x.score)
                      .Select(x => x.obj);
        }

        /// <summary>
        /// Converts a search term to Double Metaphone hash code suitable for fuzzy text searching.
        /// </summary>
        /// <param name="term">A single or multiple word search term</param>
        public static string ToDoubleMetaphoneHash(this string term)
        {
            return string.Join(" ", DoubleMetaphone.GetKeys(term));
        }
    }
}

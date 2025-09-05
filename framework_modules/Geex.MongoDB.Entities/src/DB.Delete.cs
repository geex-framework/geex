using MongoDB.Driver;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Entities.Utilities;

#pragma warning disable 618

namespace MongoDB.Entities
{
    public static partial class DB
    {
        private static readonly int deleteBatchSize = 100000;
        private static MethodInfo DeleteCascadingAsyncMethod = typeof(DB).GetMethod(nameof(DeleteCascadingAsync), BindingFlags.Static | BindingFlags.NonPublic);


        private static async Task<long> DeleteCascadingAsync<T>(IEnumerable<string> ids,
            DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            // note: cancellation should not be enabled outside of transactions because multiple collections are involved
            //       and premature cancellation could cause data inconsistencies.
            //       i.e. don't pass the cancellation token to delete methods below that don't take a session.
            //       also make consumers call ThrowIfCancellationNotSupported() before calling this method.

            //var options = new ListCollectionNamesOptions
            //{
            //    Filter = "{$and:[{name:/~/},{name:/" + CollectionName<T>() + "/}]}"
            //};

            var filter = Filter<T>().In(x => x.Id, ids);
            var tasks = new List<Task>(2);
            var delResTask =
                    dbContext?.Session == null
                    ? Collection<T>().DeleteManyAsync(filter, cancellationToken: cancellation)
                    : Collection<T>().DeleteManyAsync(dbContext?.Session, filter, null, cancellation);

            tasks.Add(delResTask);

            if (typeof(T).IsAssignableTo<FileEntity>())
            {
                var fileEntityFilter = Filter<FileChunk>().In(x => x.FileId, ids);
                tasks.Add(
                    dbContext?.Session == null
                    ? Collection<FileChunk>().DeleteManyAsync(fileEntityFilter, cancellationToken: cancellation)
                    : Collection<FileChunk>().DeleteManyAsync(dbContext?.Session, fileEntityFilter, null, cancellation));
            }
            await Task.WhenAll(tasks);
            //await Task.WhenAll(tasks).ConfigureAwait(false);
            if (dbContext != default)
            {
                var rootType = Cache<T>.RootEntityType;
                foreach (var id in ids)
                {
                    dbContext.DbDataCache[rootType].TryRemove(id, out _);
                    dbContext.MemoryDataCache[rootType].TryRemove(id, out _);
                }
            }
            var delRes = await delResTask;
            return delRes.DeletedCount;
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="id">The Id of the entity to delete</param>
        /// <param name = "session" >An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static async Task<long> DeleteAsync<T>(string id, DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            dbContext?.ThrowIfCancellationNotSupported(cancellation);
            var rootType = typeof(T).GetRootBsonClassMap().ClassType;
            return await (DeleteCascadingAsyncMethod.MakeGenericMethodFast(rootType).Invoke(null, [new[] { id }, dbContext, cancellation]) as Task<long>);
            //return DeleteCascadingAsync<T>();
        }

        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="type"></param>
        /// <param name="id">The Id of the entity to delete</param>
        /// <param name="dbContext"></param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <param name = "session" >An optional session if using within a transaction</param>
        public static async Task<long> DeleteAsync(Type type, string id, DbContext dbContext = null,
            CancellationToken cancellation = default)
        {
            dbContext?.ThrowIfCancellationNotSupported(cancellation);
            var rootType = type.GetRootBsonClassMap().ClassType;
            return await (DeleteCascadingAsyncMethod.MakeGenericMethodFast(rootType).Invoke(null, [new[] { id }, dbContext, cancellation]) as Task<long>);
            //return DeleteCascadingAsync<T>();
        }

        /// <summary>
        /// Deletes matching entities with an expression
        /// <para>HINT: If the expression matches more than 100,000 entities, they will be deleted in batches of 100k.</para>
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name = "session" >An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static async Task<long> DeleteAsync<T>(Expression<Func<T, bool>> expression, DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            dbContext?.ThrowIfCancellationNotSupported(cancellation);

            long deletedCount = 0;

            var cursor = await new Find<T, string>(dbContext)
                               .Match(expression)
                               .Project(e => e.Id)
                               .Option(o => o.BatchSize = deleteBatchSize)
                               .ExecuteCursorAsync(cancellation)
                               .ConfigureAwait(false);

            using (cursor)
            {
                while (await cursor.MoveNextAsync(cancellation).ConfigureAwait(false))
                {
                    if (cursor.Current.Any())
                    {
                        var toDeletes = cursor.Current;
                        deletedCount += await (DeleteCascadingAsyncMethod.MakeGenericMethodFast(typeof(T).GetRootBsonClassMap().ClassType).Invoke(null, [toDeletes, dbContext, cancellation]) as Task<long>);
                    }
                }
            }

            return deletedCount;
        }

        public static async Task<long> DeleteTypedAsync<T>(DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            dbContext?.ThrowIfCancellationNotSupported(cancellation);

            long deletedCount = 0;

            var cursor = await new Find<T, string>(dbContext)
                               .Project(e => e.Id)
                               .Option(o => o.BatchSize = deleteBatchSize)
                               .ExecuteCursorAsync(cancellation)
                               .ConfigureAwait(false);

            using (cursor)
            {
                while (await cursor.MoveNextAsync(cancellation).ConfigureAwait(false))
                {
                    if (cursor.Current.Any())
                    {
                        var toDeletes = cursor.Current;
                        deletedCount += await (DeleteCascadingAsyncMethod.MakeGenericMethodFast(typeof(T).GetRootBsonClassMap().ClassType).Invoke(null, [toDeletes, dbContext, cancellation]) as Task<long>);
                    }
                }
            }

            return deletedCount;
        }

        /// <summary>
        /// Deletes entities using a collection of Ids
        /// <para>HINT: If more than 100,000 Ids are passed in, they will be processed in batches of 100k.</para>
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="ids">An IEnumerable of entity Ids</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static async Task<long> DeleteAsync<T>(IEnumerable<string> ids, DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            dbContext?.ThrowIfCancellationNotSupported(cancellation);

            if (ids.Count() <= deleteBatchSize)
                return await DeleteCascadingAsync<T>(ids, dbContext, cancellation).ConfigureAwait(false);

            long deletedCount = 0;

            foreach (var batch in ids.ToBatches(deleteBatchSize))
            {
                deletedCount += (await (DeleteCascadingAsyncMethod.MakeGenericMethodFast(typeof(T).GetRootBsonClassMap().ClassType).Invoke(null, [batch, dbContext, cancellation]) as Task<long>).ConfigureAwait(false));
            }

            return deletedCount;
        }
    }
}

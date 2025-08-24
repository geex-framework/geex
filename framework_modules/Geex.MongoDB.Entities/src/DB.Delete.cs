using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

#pragma warning disable 618

namespace MongoDB.Entities
{
    public static partial class DB
    {
        private static readonly int deleteBatchSize = 100000;
        private static MethodInfo DeleteCascadingAsyncMethod = typeof(DB).GetMethod(nameof(DeleteCascadingAsync), BindingFlags.Static | BindingFlags.NonPublic);


        private static async Task<long> DeleteCascadingAsync<T>(IEnumerable<ObjectId> Ids,
            DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            // note: cancellation should not be enabled outside of transactions because multiple collections are involved
            //       and premature cancellation could cause data inconsistencies.
            //       i.e. don't pass the cancellation token to delete methods below that don't take a session.
            //       also make consumers call ThrowIfCancellationNotSupported() before calling this method.

            var db = Database<T>();
            //var options = new ListCollectionNamesOptions
            //{
            //    Filter = "{$and:[{name:/~/},{name:/" + CollectionName<T>() + "/}]}"
            //};

            var tasks = new HashSet<Task>();
            foreach (var id in Ids)
            {
                dbContext?.MemoryDataCache[typeof(T)].Remove(id, out _);
                dbContext?.DbDataCache[typeof(T)].Remove(id, out _);
            }
            var delResTask =
                    dbContext?.Session == null
                    ? Collection<T>().DeleteManyAsync(x => Ids.Contains(x.Id))
                    : Collection<T>().DeleteManyAsync(dbContext?.Session, x => Ids.Contains(x.Id), null, cancellation);

            tasks.Add(delResTask);

            if (typeof(T).IsAssignableTo<FileEntity>())
            {
                tasks.Add(
                    dbContext?.Session == null
                    ? db.GetCollection<FileChunk>(CollectionName<FileChunk>()).DeleteManyAsync(x => Ids.Contains(x.FileId))
                    : db.GetCollection<FileChunk>(CollectionName<FileChunk>()).DeleteManyAsync(dbContext?.Session, x => Ids.Contains(x.FileId), null, cancellation));
            }
            Task.WhenAll(tasks).Wait();
            //await Task.WhenAll(tasks).ConfigureAwait(false);

            var delRes = await delResTask.ConfigureAwait(false);
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
        public static async Task<long> DeleteAsync<T>(ObjectId id, DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            dbContext?.ThrowIfCancellationNotSupported(cancellation);
            var rootType = typeof(T).GetRootBsonClassMap().ClassType;
            return await (DeleteCascadingAsyncMethod.MakeGenericMethod(rootType)
                .Invoke(null, new object[] { new[] { id }, dbContext, cancellation }) as Task<long>);
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
        public static async Task<long> DeleteAsync(Type type, ObjectId id, DbContext dbContext = null,
            CancellationToken cancellation = default)
        {
            dbContext?.ThrowIfCancellationNotSupported(cancellation);
            var rootType = type.GetRootBsonClassMap().ClassType;
            return await (DeleteCascadingAsyncMethod.MakeGenericMethod(rootType)
                .Invoke(null, new object[] { new[] { id }, dbContext, cancellation }) as Task<long>);
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

            var cursor = await DB.Find<T, string>(dbContext)
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
                        deletedCount += await (DeleteCascadingAsyncMethod
                            .MakeGenericMethod(typeof(T).GetRootBsonClassMap().ClassType)
                            .Invoke(null, new object[] { toDeletes, dbContext, cancellation }) as Task<long>);
                    }
                }
            }

            return deletedCount;
        }

        public static async Task<long> DeleteAsync<T>(DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            dbContext?.ThrowIfCancellationNotSupported(cancellation);

            long deletedCount = 0;

            var cursor = await DB.Find<T, string>(dbContext)
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
                        deletedCount += await (DeleteCascadingAsyncMethod
                            .MakeGenericMethod(typeof(T).GetRootBsonClassMap().ClassType)
                            .Invoke(null, new object[] { toDeletes, dbContext, cancellation }) as Task<long>);
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
        /// <param name="Ids">An IEnumerable of entity Ids</param>
        /// <param name = "session" > An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static async Task<long> DeleteAsync<T>(IEnumerable<ObjectId> Ids, DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            dbContext?.ThrowIfCancellationNotSupported(cancellation);

            if (Ids.Count() <= deleteBatchSize)
                return await DeleteCascadingAsync<T>(Ids, dbContext, cancellation).ConfigureAwait(false);

            long deletedCount = 0;

            foreach (var batch in Ids.ToBatches(deleteBatchSize))
            {
                deletedCount += (await ((DeleteCascadingAsyncMethod
                            .MakeGenericMethod(typeof(T).GetRootBsonClassMap().ClassType)
                            .Invoke(null, new object[] { batch, dbContext, cancellation }) as Task<long>)).ConfigureAwait(false));
            }

            return deletedCount;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Geex.Notifications;
using Geex.Storage;

using MongoDB.Driver;
using MongoDB.Entities;


// ReSharper disable once CheckNamespace
namespace Geex.Abstractions
{
    public static class GeexCommonAbstractionStorageExtensions
    {
        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static async Task<long> DeleteAsync<T>(this T entity) where T : IEntityBase
        {
            (entity as IEntity)?.AddDomainEvent(new EntityDeletedEvent<T>(entity.Id));
            (entity.DbContext)?.Detach(entity);
            return await entity.DeleteAsync();
        }

        public static async Task<long> DeleteAsync<T>(this IEnumerable<T> entities) where T : IEntityBase
        {
            var enumerable = entities.ToList();
            foreach (var entity in enumerable)
            {
                (entity as IEntity)?.AddDomainEvent(new EntityDeletedEvent<T>(entity.Id));
            }
            var deletes = enumerable.Select(async x =>
            {
                (x.DbContext)?.Detach(x);
                return await x.DeleteAsync();
            });
            // todo: possible deadlock for duplicate delete in parallel
            var result = await Task.WhenAll(deletes);
            return result.Sum();
        }

        /// <summary>
        /// Saves a complete entity replacing an existing entity or creating a new one if it does not exist.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is replaced.
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<ReplaceOneResult> SaveAsync<T>(this T entity, CancellationToken cancellation = default) where T : IEntityBase
        {
            (entity.DbContext)?.Detach<T>(entity);
            return DB.SaveAsync(entity, cancellation: cancellation);
        }

        /// <summary>
        /// Saves a batch of complete entities replacing existing ones or creating new ones if they do not exist.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is replaced.
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<BulkWriteResult<T>> SaveAsync<T>(this List<T> entities, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            var dbContext = entities.FirstOrDefault()?.DbContext;
            if (entities.Any(x => x.DbContext?.Session != dbContext?.Session))
            {
                throw new InvalidOperationException("bulksave entities should be in the same session");
            }
            (dbContext)?.Detach(entities);

            return DB.SaveAsync(entities, dbContext, cancellation);
        }

        /// <summary>
        /// Saves a batch of complete entities replacing existing ones or creating new ones if they do not exist.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is replaced.
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<BulkWriteResult<T>> SaveAsync<T>(this IEnumerable<T> entities, IClientSessionHandle session = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            return entities.ToList().SaveAsync(session, cancellation);
        }

        /// <summary>
        /// Saves an entity partially with only the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntityBase</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<UpdateResult> SaveOnlyAsync<T>(this T entity, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntityBase
        {
            (entity.DbContext)?.Detach(entity);
            return DB.SaveOnlyAsync(entity, members, entity.DbContext, cancellation);
        }

        /// <summary>
        /// Saves a batch of entities partially with only the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntityBase</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<BulkWriteResult<T>> SaveOnlyAsync<T>(this IEnumerable<T> entities, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntityBase
        {
            var enumerable = entities.ToList();
            var dbContext = enumerable.FirstOrDefault()?.DbContext;
            (dbContext)?.Detach(entities);
            return DB.SaveOnlyAsync(enumerable, members, dbContext, cancellation);
        }

        /// <summary>
        /// Saves an entity partially excluding the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntityBase</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<UpdateResult> SaveExceptAsync<T>(this T entity, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntityBase
        {
            (entity.DbContext)?.Detach(entity);
            return DB.SaveExceptAsync(entity, members, entity.DbContext, cancellation);
        }

        /// <summary>
        /// Saves a batch of entities partially excluding the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntityBase</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<BulkWriteResult<T>> SaveExceptAsync<T>(this IEnumerable<T> entities, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntityBase
        {
            var enumerable = entities.ToList();
            var dbContext = enumerable.FirstOrDefault()?.DbContext;
            (dbContext)?.Detach(entities);
            return DB.SaveExceptAsync(enumerable, members, dbContext, cancellation);
        }
    }
}

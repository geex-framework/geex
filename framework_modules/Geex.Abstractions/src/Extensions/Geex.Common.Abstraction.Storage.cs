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
        /// Deletes multiple entities from MongoDB using batch operation for optimal performance.
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>遵循Entity=>DbContext=>DB的调用顺序，统一Detach时机以保证批量操作性能</para>
        /// </summary>
        public static async Task<long> DeleteAsync<T>(this IEnumerable<T> entities) where T : IEntityBase
        {
            var enumerable = entities.ToList();
            if (!enumerable.Any()) return 0;

            // 为每个实体添加领域事件
            foreach (var entity in enumerable)
            {
                (entity as IEntity)?.AddDomainEvent(new EntityDeletedEvent<T>(entity.Id));
            }

            // 获取第一个实体的DbContext作为批量操作的上下文
            var dbContext = enumerable.FirstOrDefault()?.DbContext;

            if (dbContext != null)
            {
                // 遵循Entity=>DbContext=>DB的顺序，先统一Detach所有实体
                dbContext.Detach(enumerable);

                // 调用DbContext的批量删除方法，实现完整的Entity=>DbContext=>DB调用链
                return await dbContext.DeleteAsync(enumerable);
            }
            else
            {
                // 没有DbContext时，直接使用DB层批量删除
                var entityIds = enumerable.Select(e => e.Id).Where(id => !string.IsNullOrEmpty(id)).ToList();
                if (entityIds.Any())
                {
                    return await DB.DeleteAsync<T>(entityIds);
                }
            }

            return 0;
        }

        /// <summary>
        /// Saves a complete entity replacing an existing entity or creating a new one if it does not exist.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is replaced.
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public static Task<WriteResult> SaveAsync<T>(this T entity, CancellationToken cancellation = default) where T : IEntityBase
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
        public static Task<BulkWriteResult<T>> SaveAsync<T>(this IEnumerable<T> entities, DbContext dbContext = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            (dbContext)?.Detach(entities);
            return DB.SaveAsync(entities, dbContext, cancellation);
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
        public static Task<WriteResult> SaveOnlyAsync<T>(this T entity, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntityBase
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
        public static Task<WriteResult> SaveExceptAsync<T>(this T entity, Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntityBase
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

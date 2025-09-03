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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Storage;

using MongoDB.Driver;
using MongoDB.Entities;


// ReSharper disable once CheckNamespace
namespace Geex.Common.Abstraction
{
    public static class GeexCommonAbstractionStorageExtensions
    {
        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static Task<DeleteResult> DeleteAsync<T>(this T entity) where T : Storage.Entity<T>
        {
            entity.AddDomainEvent(new EntityDeletedNotification<T>(entity));
            return DB.DeleteAsync<T>(entity.Id, (entity as IEntityBase).DbContext);
        }

        public static Task<DeleteResult> DeleteAsync<T>(this IEnumerable<T> entities) where T : Storage.Entity<T>
        {
            var enumerable = entities.ToList();
            foreach (var entity in enumerable)
            {
                entity.AddDomainEvent(new EntityDeletedNotification<T>(entity));
            }
            return DB.DeleteAsync<T>(enumerable.Select(e => e.Id), (enumerable.FirstOrDefault() as IEntityBase)?.DbContext);
        }
    }
}

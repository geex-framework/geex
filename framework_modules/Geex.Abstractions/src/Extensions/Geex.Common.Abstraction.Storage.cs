using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geex.Common.Notifications;
using Geex.Abstractions.Storage;

using MongoDB.Driver;


// ReSharper disable once CheckNamespace
namespace Geex.Abstractions
{
    public static class GeexCommonAbstractionStorageExtensions
    {
        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static async Task<long> DeleteAsync<T>(this T entity) where T : Storage.Entity<T>
        {
            entity.AddDomainEvent(new EntityDeletedNotification<T>(entity.Id));
            return await entity.DeleteAsync();
        }

        public static async Task<long> DeleteAsync<T>(this IEnumerable<T> entities) where T : Storage.Entity<T>
        {
            var enumerable = entities.ToList();
            foreach (var entity in enumerable)
            {
                entity.AddDomainEvent(new EntityDeletedNotification<T>(entity.Id));
            }
            var deletes = enumerable.Select(async x => await x.DeleteAsync());
            // todo: possible deadlock for duplicate delete in parallel
            var result = await Task.WhenAll(deletes);
            return result.Sum();
        }
    }
}

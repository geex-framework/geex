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
        public static DeleteResult Merge(this DeleteResult result, DeleteResult anotherResult)
        {
            if (result.IsAcknowledged && anotherResult.IsAcknowledged)
            {
                return new DeleteResult.Acknowledged(result.DeletedCount + anotherResult.DeletedCount);
            }
            throw new Exception($"bulk deletion failed. expected: {result.DeletedCount + anotherResult.DeletedCount}, actual:{(result.IsAcknowledged ? result.DeletedCount : 0) + (anotherResult.IsAcknowledged ? anotherResult.DeletedCount : 0)}");
        }
        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        public static async Task<DeleteResult> DeleteAsync<T>(this T entity) where T : Storage.Entity<T>
        {
            entity.AddDomainEvent(new EntityDeletedNotification<T>(entity));
            return await entity.DeleteAsync();
        }

        public static async Task<DeleteResult> DeleteAsync<T>(this IEnumerable<T> entities) where T : Storage.Entity<T>
        {
            var enumerable = entities.ToList();
            foreach (var entity in enumerable)
            {
                entity.AddDomainEvent(new EntityDeletedNotification<T>(entity));
            }
            var deletes = enumerable.Select(async x => await x.DeleteAsync());
            var result = await Task.WhenAll(deletes);
            if (result.All(x => x.IsAcknowledged))
            {
                return new DeleteResult.Acknowledged(result.Sum(x => x.DeletedCount));
            }
            throw new Exception($"bulk deletion failed. expected: {result.Sum(x => x.DeletedCount)}, actual:{result.Where(x => x.IsAcknowledged).Sum(x => x.DeletedCount)}");
        }
    }
}

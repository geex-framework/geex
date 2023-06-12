using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Misc;
using MongoDB.Entities;

// ReSharper disable once CheckNamespace
namespace MongoDB.Driver
{
    public static class MongoCollectionExtensions
    {
        public static TDocument First<TDocument>(
            this IMongoCollection<TDocument> collection,
            Expression<Func<TDocument, bool>> filter,
            FindOptions options = null)
        {
            Ensure.IsNotNull(collection, nameof(collection));
            Ensure.IsNotNull(filter, nameof(filter));
            return collection.Find(new ExpressionFilterDefinition<TDocument>(filter), options).First();
        }

        public static TDocument FirstOrDefault<TDocument>(
            this IMongoCollection<TDocument> collection,
            Expression<Func<TDocument, bool>> filter,
            FindOptions options = null)
        {
            Ensure.IsNotNull(collection, nameof(collection));
            Ensure.IsNotNull(filter, nameof(filter));
            return collection.Find(new ExpressionFilterDefinition<TDocument>(filter), options).FirstOrDefault();
        }

        public static Task<TDocument> FirstAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            Expression<Func<TDocument, bool>> filter,
            FindOptions options = null)
        {
            Ensure.IsNotNull(collection, nameof(collection));
            Ensure.IsNotNull(filter, nameof(filter));
            return collection.Find(new ExpressionFilterDefinition<TDocument>(filter), options).FirstAsync();
        }

        public static Task<TDocument> FirstOrDefaultAsync<TDocument>(
            this IMongoCollection<TDocument> collection,
            Expression<Func<TDocument, bool>> filter,
            FindOptions options = null)
        {
            Ensure.IsNotNull(collection, nameof(collection));
            Ensure.IsNotNull(filter, nameof(filter));
            return collection.Find(new ExpressionFilterDefinition<TDocument>(filter), options).FirstOrDefaultAsync();
        }

        public static Task<TDocument> FirstOrDefaultAsync<TDocument>(this IMongoCollection<TDocument> collection, ObjectId id) where TDocument : IEntityBase
        {
            return collection.FirstOrDefaultAsync(id.ToString());
        }
        public static Task<TDocument> FirstOrDefaultAsync<TDocument>(this IMongoCollection<TDocument> collection, string id) where TDocument : IEntityBase
        {
            Ensure.IsNotNull(collection, nameof(collection));
            Ensure.IsNotNull(id, nameof(id));
            return collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }
    }
}

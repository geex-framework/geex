using System;
using System.Collections.Concurrent;
using MongoDB.Bson.Serialization;

namespace MongoDB.Entities.Utilities
{
    public class DbContextCache
    {
        public ConcurrentDictionary<Type, ConcurrentDictionary<string, IEntityBase>> TypedCacheDictionary { get; set; } = new();
        public ConcurrentDictionary<string, IEntityBase> this[Type index]
        {
            get
            {
                var rootClassType = index.GetRootBsonClassMap().ClassType;
                if (!TypedCacheDictionary.ContainsKey(rootClassType))
                {
                    TypedCacheDictionary[rootClassType] = new ConcurrentDictionary<string, IEntityBase>();
                }
                return TypedCacheDictionary[rootClassType];
            }
        }

        /// <summary>
        /// Get entity object from cache, if not found, return null
        /// </summary>
        /// <typeparam name="T">the type to cast the entity</typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public T? Get<T>(string id) where T : class, IEntityBase
        {
            var type = typeof(T).GetRootBsonClassMap().ClassType;
            if (TypedCacheDictionary.TryGetValue(type, out var cacheDictionary) && cacheDictionary.TryGetValue(id, out var entity))
            {
                return entity as T;
            }
            return default;
        }
    }
}

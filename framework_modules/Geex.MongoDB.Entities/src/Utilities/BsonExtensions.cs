using System;
using System.Collections.Concurrent;
using MongoDB.Entities;
using MongoDB.Entities.Utilities;

// ReSharper disable once CheckNamespace
namespace MongoDB.Bson.Serialization
{
    public static class BsonExtensions
    {
        public static BsonClassMap GetRootBsonClassMap(this Type entityType)
        {
            if (entityType.IsInterface)
            {
                return DB.InterfaceCache[entityType];
            }
            if (DB.InheritanceCache.TryGetValue(entityType, out var rootClassMap))
            {
                return rootClassMap;
            }
            else
            {
                var classMap = BsonClassMap.LookupClassMap(entityType);
                if (classMap.HasRootClass && !classMap.IsRootClass)
                {
                    while (!classMap.BaseClassMap.IsRootClass)
                    {
                        classMap = classMap.BaseClassMap;
                    }
                    rootClassMap = classMap.BaseClassMap;
                    DB.InheritanceCache[entityType] = rootClassMap;
                    if (!DB.InheritanceTreeCache.TryGetValue(rootClassMap.ClassType, out var dictionary))
                    {
                        dictionary = new ConcurrentDictionary<string, Type>();
                        DB.InheritanceTreeCache[rootClassMap.ClassType] = dictionary;
                    }
                    dictionary[entityType.Name] = entityType;
                    return rootClassMap;
                }
                else
                {
                    DB.InheritanceCache[entityType] = classMap;
                    if (!DB.InheritanceTreeCache.TryGetValue(classMap.ClassType, out var dictionary))
                    {
                        dictionary = new ConcurrentDictionary<string, Type>();
                        DB.InheritanceTreeCache[classMap.ClassType] = dictionary;
                    }
                    dictionary[entityType.Name] = entityType;
                    return classMap;
                }
            }
        }

        public static void Inherit<T>(this BsonClassMap bsonClassMap)
        {
            bsonClassMap.MapInheritance<T>();
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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

        /// <summary>
        /// 获取完整的继承链判别器数组，从根类型到当前类型
        /// </summary>
        /// <param name="entityType">实体类型</param>
        /// <returns>继承链的判别器数组</returns>
        public static BsonValue GetBsonDiscriminators(this Type entityType)
        {
            var discriminators = new List<BsonValue>();
            var currentType = entityType;

            // 从当前类型向上遍历到根类型
            while (currentType != null && typeof(IEntityBase).IsAssignableFrom(currentType))
            {
                try
                {
                    var classMap = BsonClassMap.LookupClassMap(currentType);
                    if (classMap?.Discriminator != null)
                    {
                        discriminators.Add(classMap.Discriminator);
                    }
                }
                catch
                {
                    // 如果无法获取ClassMap，使用类型名称作为判别器
                    discriminators.Add(currentType.Name);
                }

                // 获取父类型
                currentType = currentType.BaseType;

                // 如果到达了基本的Entity类型或object类型，停止遍历
                if (currentType == typeof(object) ||
                    currentType?.Name.StartsWith("Entity`") == true ||
                    currentType?.Name.StartsWith("EntityBase`") == true)
                {
                    break;
                }
            }

            // 反转数组，使其从根类型到当前类型的顺序
            discriminators.Reverse();
            var bsonArray = new BsonArray(discriminators);
            var result = bsonArray.Count == 1 ? bsonArray[0] : (BsonValue)bsonArray;
            return result;
        }

        public static void Inherit<T>(this BsonClassMap bsonClassMap)
        {
            bsonClassMap.MapInheritance<T>();
        }
    }
}

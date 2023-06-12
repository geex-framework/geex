using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.Bson.Serialization;

namespace MongoDB.Entities.Utilities
{
    internal static class TypeException
    {
        public static List<Type> GetDirectInterfaces(this Type type)
        {
            var allInterfaces = type.GetInterfaces();
            return allInterfaces
                .Except(allInterfaces.SelectMany(t => t.GetInterfaces()))
                .Except(type.GetBaseClasses(typeof(object), false).SelectMany(x => x.GetInterfaces()))
                .ToList();
        }

        public static void MapInheritance(this BsonClassMap classMap)
        {
            var classType = classMap.ClassType;
            var className = classType.Name;
            var baseClasses = classType.GetBaseClasses(typeof(object), false)
                .Where(x => !x.Name.StartsWith("Entity`"))
                .Where(x => !x.Name.StartsWith("EntityBase`"))
                ;
            if (classType.IsAssignableTo<IEntityBase>())
            {
                var interfaces = classType.GetDirectInterfaces().Except(new[] { typeof(IEntityBase) });
                foreach (var @interface in interfaces)
                {
                    DB.InterfaceCache[@interface] = classMap;
                }

                if (baseClasses.Any())
                {
                    var rootType = baseClasses.FirstOrDefault();
                    if (rootType != default)
                    {
                        var rootClassMap = BsonClassMap.LookupClassMap(rootType);
                        DB.InheritanceCache[classType] = rootClassMap;
                        if (!DB.InheritanceTreeCache.TryGetValue(rootType, out var dictionary))
                        {
                            dictionary = new ConcurrentDictionary<string, Type>();
                            DB.InheritanceTreeCache[rootType] = dictionary;
                        }
                        dictionary[classType.Name] = classType;
                    }
                    else
                    {
                        if (!classMap.IsRootClass)
                        {
                            classMap.SetIsRootClass(true);
                        }
                    }
                }
                else
                {
                    if (!classMap.IsRootClass)
                    {
                        classMap.SetIsRootClass(true);
                    }
                }
            }
        }
    }
}

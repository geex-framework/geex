using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace MongoDB.Entities.Utilities
{
    /// <summary>
    /// 在 <c>ConfigLazyQuery</c> 时登记实体类型与导航属性名，供 BatchLoad 注册/校验使用，避免运行时反射实例化实体。
    /// </summary>
    public static class LazyQueryMetadataRegistry
    {
        private static readonly ConcurrentDictionary<(Type Type, string PropertyName), byte> Registered =
            new();

        public static void Register(Type declaringType, string propertyName)
        {
            if (declaringType == null || string.IsNullOrEmpty(propertyName))
            {
                return;
            }

            Registered[(declaringType, propertyName)] = 0;

            if (declaringType.IsInterface)
            {
                return;
            }

            foreach (var iface in declaringType.GetInterfaces())
            {
                if (iface.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy) != null)
                {
                    Registered[(iface, propertyName)] = 0;
                }
            }
        }

        public static bool IsRegistered(Type entityType, string propertyName)
        {
            if (entityType == null || string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            if (Registered.ContainsKey((entityType, propertyName)))
            {
                return true;
            }

            if (entityType.IsInterface)
            {
                return false;
            }

            foreach (var iface in entityType.GetInterfaces())
            {
                if (Registered.ContainsKey((iface, propertyName)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

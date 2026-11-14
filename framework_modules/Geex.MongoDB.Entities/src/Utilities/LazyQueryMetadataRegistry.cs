using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace MongoDB.Entities.Utilities
{
    /// <summary>
    /// 在首次规划关联查询前完成实体的导航配置, 查询条件和数据源仍保留在各自实例中.
    /// </summary>
    public static class LazyQueryMetadataRegistry
    {
        private static readonly ConcurrentDictionary<(Type Type, string PropertyName), byte> Registered =
            new();
        private static readonly ConcurrentDictionary<Type, Lazy<bool>> Initializers = new();
        private static readonly object InitializationLock = new();
        [ThreadStatic]
        private static HashSet<(Type Type, string PropertyName)>? PendingRegistrations;

        public static void Register(Type declaringType, string propertyName)
        {
            if (declaringType == null || string.IsNullOrEmpty(propertyName))
            {
                return;
            }

            RegisterKey(declaringType, propertyName);

            if (declaringType.IsInterface)
            {
                return;
            }

            foreach (var iface in declaringType.GetInterfaces())
            {
                if (iface.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy) != null)
                {
                    RegisterKey(iface, propertyName);
                }
            }
        }

        private static void RegisterKey(Type declaringType, string propertyName)
        {
            if (PendingRegistrations is { } pending)
            {
                pending.Add((declaringType, propertyName));
            }
            else
            {
                Registered[(declaringType, propertyName)] = 0;
            }
        }

        public static bool IsRegistered(Type entityType, string propertyName)
        {
            if (entityType == null || string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            if (entityType.IsInterface && DB.InterfaceCache.TryGetValue(entityType, out var classMap))
            {
                entityType = classMap.ClassType;
            }

            if (!entityType.IsAbstract && !entityType.ContainsGenericParameters &&
                typeof(IEntityBase).IsAssignableFrom(entityType))
            {
                EnsureInitialized(entityType, propertyName);
            }

            return Registered.ContainsKey((entityType, propertyName));
        }

        private static void EnsureInitialized(Type entityType, string propertyName)
        {
            if (Initializers.TryGetValue(entityType, out var initialized) && initialized.IsValueCreated)
            {
                return;
            }

            // 冷初始化先取得共同锁, 避免构造函数查询其他类型时产生反向的类型锁等待.
            lock (InitializationLock)
            {
                if (!Initializers.TryGetValue(entityType, out var initializer))
                {
                    if (Registered.ContainsKey((entityType, propertyName)))
                    {
                        return;
                    }

                    initializer = new Lazy<bool>(() => Initialize(entityType), LazyThreadSafetyMode.ExecutionAndPublication);
                    Initializers[entityType] = initializer;
                }

                _ = initializer.Value;
            }
        }

        private static bool Initialize(Type entityType)
        {
            var constructor = entityType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null, Type.EmptyTypes, modifiers: null);
            if (constructor == null)
            {
                throw new InvalidOperationException(
                    $"Cannot initialize LazyQuery metadata for '{entityType.FullName}': " +
                    "configure ConfigLazyQuery in a parameterless constructor or explicitly register the navigation metadata.");
            }

            var parentRegistrations = PendingRegistrations;
            var registrations = new HashSet<(Type Type, string PropertyName)>();
            PendingRegistrations = registrations;
            try
            {
                var entity = (IEntityBase)constructor.Invoke(null);
                foreach (var propertyName in entity.LazyQueryCache.Keys)
                {
                    Register(entityType, propertyName);
                }

                // 构造完成后才发布, 防止接口或基类看到失败构造留下的部分配置.
                foreach (var registration in registrations)
                {
                    Registered[registration] = 0;
                }
                return true;
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                throw new InvalidOperationException(
                    $"Failed to initialize LazyQuery metadata for '{entityType.FullName}'.", exception.InnerException);
            }
            finally
            {
                PendingRegistrations = parentRegistrations;
            }
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace System.Reflection
{
    public static class PropertyReflectionCache
    {
        /// <summary>
        /// 高性能缓存键结构体，避免元组的GC压力
        /// </summary>
        internal readonly struct TypeKey : IEquatable<TypeKey>
        {
            public readonly string PropertyName;
            public readonly Type Type;
            public readonly int HashCode;

            public TypeKey(Type type, string propertyName)
            {
                PropertyName = propertyName;
                Type = type;
                HashCode = System.HashCode.Combine(type, propertyName);
            }

            public bool Equals(TypeKey other) => Type == other.Type && PropertyName == other.PropertyName;
            public override bool Equals(object obj) => obj is TypeKey key && Equals(key);
            public override int GetHashCode() => HashCode;
        }
        private static readonly ConcurrentDictionary<TypeKey, PropertyInfo> _propertyCache = new ConcurrentDictionary<TypeKey, PropertyInfo>();
        /// <summary>
        /// 获取多个泛型参数的泛型方法（缓存版本）
        /// </summary>
        public static PropertyInfo GetPropertyFast(this Type type, string propertyName)
        {
            var key = new TypeKey(type, propertyName);
            return _propertyCache.GetOrAdd(key, k => k.Type.GetProperty(propertyName));
        }

    }
}

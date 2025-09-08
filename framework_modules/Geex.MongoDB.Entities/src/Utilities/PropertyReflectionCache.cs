using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

using FastExpressionCompiler;

using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities.Utilities
{
    /// <summary>
    /// 高性能属性键结构体，避免元组GC压力
    /// </summary>
    internal readonly struct PropertyKey : IEquatable<PropertyKey>
    {
        public readonly Type GenericType;
        public readonly Type TypeArgument;
        public readonly string PropertyName;
        public readonly int HashCode;

        public PropertyKey(Type genericType, Type typeArgument, string propertyName)
        {
            GenericType = genericType;
            TypeArgument = typeArgument;
            PropertyName = propertyName;
            HashCode = System.HashCode.Combine(genericType, typeArgument, propertyName);
        }

        public bool Equals(PropertyKey other) =>
            GenericType == other.GenericType &&
            TypeArgument == other.TypeArgument &&
            PropertyName == other.PropertyName;

        public override bool Equals(object obj) => obj is PropertyKey key && Equals(key);
        public override int GetHashCode() => HashCode;
    }

    /// <summary>
    /// 双类型键结构体
    /// </summary>
    internal readonly struct TypePairKey : IEquatable<TypePairKey>
    {
        public readonly Type Type1;
        public readonly Type Type2;
        public readonly int HashCode;

        public TypePairKey(Type type1, Type type2)
        {
            Type1 = type1;
            Type2 = type2;
            HashCode = System.HashCode.Combine(type1, type2);
        }

        public bool Equals(TypePairKey other) => Type1 == other.Type1 && Type2 == other.Type2;
        public override bool Equals(object obj) => obj is TypePairKey key && Equals(key);
        public override int GetHashCode() => HashCode;
    }

    /// <summary>
    /// 类型字符串键结构体
    /// </summary>
    internal readonly struct TypeStringKey : IEquatable<TypeStringKey>
    {
        public readonly Type Type;
        public readonly string Text;
        public readonly int HashCode;

        public TypeStringKey(Type type, string text)
        {
            Type = type;
            Text = text;
            HashCode = System.HashCode.Combine(type, text);
        }

        public bool Equals(TypeStringKey other) => Type == other.Type && Text == other.Text;
        public override bool Equals(object obj) => obj is TypeStringKey key && Equals(key);
        public override int GetHashCode() => HashCode;
    }

    /// <summary>
    /// 属性反射缓存，用于高性能的泛型类型属性访问
    /// </summary>
    public static class PropertyReflectionCache
    {
        /// <summary>
        /// 泛型类型属性访问器缓存 - 使用高性能键结构体
        /// </summary>
        private static readonly ConcurrentDictionary<PropertyKey, Func<object, object>>
            _propertyAccessorCache = new ConcurrentDictionary<PropertyKey, Func<object, object>>();

        /// <summary>
        /// 泛型类型信息缓存 - 使用高性能键结构体
        /// </summary>
        private static readonly ConcurrentDictionary<TypePairKey, Type>
            _genericTypeCache = new ConcurrentDictionary<TypePairKey, Type>();

        /// <summary>
        /// 属性信息缓存 - 使用高性能键结构体
        /// </summary>
        private static readonly ConcurrentDictionary<TypeStringKey, PropertyInfo>
            _propertyInfoCache = new ConcurrentDictionary<TypeStringKey, PropertyInfo>();

        /// <summary>
        /// 高性能获取泛型类型的属性值
        /// </summary>
        /// <param name="genericTypeDefinition">泛型类型定义，如typeof(List&lt;&gt;)</param>
        /// <param name="typeArgument">泛型参数类型</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="instance">实例对象</param>
        /// <returns>属性值</returns>
        public static object GetGenericPropertyValue(Type genericTypeDefinition, Type typeArgument,
            string propertyName, object instance)
        {
            var key = new PropertyKey(genericTypeDefinition, typeArgument, propertyName);
            var accessor = _propertyAccessorCache.GetOrAdd(key, CreatePropertyAccessor);
            return accessor(instance);
        }

        /// <summary>
        /// 高性能获取泛型类型的属性值（强类型版本）
        /// </summary>
        public static TResult GetGenericPropertyValue<TResult>(Type genericTypeDefinition, Type typeArgument,
            string propertyName, object instance)
        {
            var value = GetGenericPropertyValue(genericTypeDefinition, typeArgument, propertyName, instance);
            return value is TResult result ? result : default(TResult);
        }

        /// <summary>
        /// 获取泛型类型（缓存版本）
        /// </summary>
        public static Type GetGenericType(Type genericTypeDefinition, Type typeArgument)
        {
            var key = new TypePairKey(genericTypeDefinition, typeArgument);
            return _genericTypeCache.GetOrAdd(key, k => k.Type1.MakeGenericType(k.Type2));
        }

        /// <summary>
        /// 获取属性信息（缓存版本）
        /// </summary>
        public static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            var key = new TypeStringKey(type, propertyName);
            return _propertyInfoCache.GetOrAdd(key, k => k.Type.GetProperty(k.Text));
        }

        /// <summary>
        /// 创建编译的属性访问器
        /// </summary>
        private static Func<object, object> CreatePropertyAccessor(PropertyKey key)
        {
            // 获取具体的泛型类型
            var concreteType = GetGenericType(key.GenericType, key.TypeArgument);
            var propertyInfo = GetPropertyInfo(concreteType, key.PropertyName);

            if (propertyInfo == null)
            {
                return _ => null;
            }

            // 创建表达式：(object instance) => ((ConcreteType)instance).PropertyName
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var typedInstance = Expression.Convert(instanceParam, concreteType);
            var propertyAccess = Expression.Property(typedInstance, propertyInfo);
            var boxedResult = Expression.Convert(propertyAccess, typeof(object));

            var lambda = Expression.Lambda<Func<object, object>>(boxedResult, instanceParam);
            return lambda.CompileFast();
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            _propertyAccessorCache.Clear();
            _genericTypeCache.Clear();
            _propertyInfoCache.Clear();
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using FastExpressionCompiler;

// ReSharper disable once CheckNamespace
namespace System.Reflection
{
    /// <summary>
    /// 泛型方法调用缓存，用于优化高频的泛型方法调用
    /// </summary>
    public static class TypeReflectionCache
    {
        /// <summary>
        /// 高性能缓存键结构体，避免元组的GC压力
        /// </summary>
        internal readonly struct TypeKey : IEquatable<TypeKey>
        {
            public readonly Type Type;
            public readonly Type ArgumentType;
            public readonly int HashCode;

            public TypeKey(Type type, Type argumentType)
            {
                Type = type;
                ArgumentType = argumentType;
                HashCode = System.HashCode.Combine(type, argumentType);
            }

            public bool Equals(TypeKey other) => Type == other.Type && ArgumentType == other.ArgumentType;
            public override bool Equals(object obj) => obj is TypeKey key && Equals(key);
            public override int GetHashCode() => HashCode;
        }

        /// <summary>
        /// 多参数方法键结构体
        /// </summary>
        internal readonly struct MultiTypeKey : IEquatable<MultiTypeKey>
        {
            public readonly Type Type;
            public readonly Type[] ArgumentTypes;
            public readonly int HashCode;

            public MultiTypeKey(Type type, Type[] argumentTypes)
            {
                Type = type;
                ArgumentTypes = argumentTypes;

                // 优化HashCode计算
                var hash = type.GetHashCode();
                for (int i = 0; i < argumentTypes.Length; i++)
                {
                    hash = System.HashCode.Combine(hash, argumentTypes[i]);
                }
                HashCode = hash;
            }

            public bool Equals(MultiTypeKey other)
            {
                if (Type != other.Type || ArgumentTypes.Length != other.ArgumentTypes.Length)
                    return false;

                for (int i = 0; i < ArgumentTypes.Length; i++)
                {
                    if (ArgumentTypes[i] != other.ArgumentTypes[i])
                        return false;
                }
                return true;
            }

            public override bool Equals(object obj) => obj is MultiTypeKey key && Equals(key);
            public override int GetHashCode() => HashCode;
        }
        private static readonly ConcurrentDictionary<TypeKey, Type> _typeCache =
            new ConcurrentDictionary<TypeKey, Type>();

        private static readonly ConcurrentDictionary<MultiTypeKey, Type> _multiParamTypeCache =
            new ConcurrentDictionary<MultiTypeKey, Type>();

        /// <summary>
        /// 高性能构造函数缓存，避免重复反射调用
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Func<object>> _constructorCache = new ConcurrentDictionary<Type, Func<object>>();

        /// <summary>
        /// 高性能无参构造函数调用
        /// </summary>
        public static object CreateInstanceFast(this Type type)
        {
            var constructor = _constructorCache.GetOrAdd(type, CreateConstructorInvoker);
            return constructor();
        }

        private static Func<object> CreateConstructorInvoker(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                throw new InvalidOperationException($"Type {type.Name} does not have a parameterless constructor.");
            }

            var newExpression = Expression.New(constructor);
            var lambda = Expression.Lambda<Func<object>>(Expression.Convert(newExpression, typeof(object)));
            return lambda.CompileFast();
        }

        /// <summary>
        /// 获取多个泛型参数的泛型方法（缓存版本）
        /// </summary>
        public static Type MakeGenericTypeFast(this Type genericTypeDefinition, params Type[] typeArguments)
        {
            if (typeArguments.Length == 1)
            {
                var key = new TypeKey(genericTypeDefinition, typeArguments[0]);
                return _typeCache.GetOrAdd(key, k => k.Type.MakeGenericType(k.ArgumentType));
            }
            else
            {
                var key = new MultiTypeKey(genericTypeDefinition, typeArguments);
                return _multiParamTypeCache.GetOrAdd(key, k => k.Type.MakeGenericType(k.ArgumentTypes));
            }
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            _typeCache.Clear();
            _multiParamTypeCache.Clear();
            _constructorCache.Clear();
        }
    }
}

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
    /// 高性能缓存键结构体，避免元组的GC压力
    /// </summary>
    internal readonly struct MethodTypeKey : IEquatable<MethodTypeKey>
    {
        public readonly MethodInfo Method;
        public readonly Type Type;
        public readonly int HashCode;

        public MethodTypeKey(MethodInfo method, Type type)
        {
            Method = method;
            Type = type;
            HashCode = System.HashCode.Combine(method, type);
        }

        public bool Equals(MethodTypeKey other) => Method == other.Method && Type == other.Type;
        public override bool Equals(object obj) => obj is MethodTypeKey key && Equals(key);
        public override int GetHashCode() => HashCode;
    }

    /// <summary>
    /// 多参数方法键结构体
    /// </summary>
    internal readonly struct MultiMethodTypeKey : IEquatable<MultiMethodTypeKey>
    {
        public readonly MethodInfo Method;
        public readonly Type[] Types;
        public readonly int HashCode;

        public MultiMethodTypeKey(MethodInfo method, Type[] types)
        {
            Method = method;
            Types = types;

            // 优化HashCode计算
            var hash = method.GetHashCode();
            for (int i = 0; i < types.Length; i++)
            {
                hash = System.HashCode.Combine(hash, types[i]);
            }
            HashCode = hash;
        }

        public bool Equals(MultiMethodTypeKey other)
        {
            if (Method != other.Method || Types.Length != other.Types.Length)
                return false;

            for (int i = 0; i < Types.Length; i++)
            {
                if (Types[i] != other.Types[i])
                    return false;
            }
            return true;
        }

        public override bool Equals(object obj) => obj is MultiMethodTypeKey key && Equals(key);
        public override int GetHashCode() => HashCode;
    }

    /// <summary>
    /// 泛型方法调用缓存，用于优化高频的泛型方法调用
    /// </summary>
    public static class MethodReflectionCache
    {
        private static readonly ConcurrentDictionary<MethodTypeKey, MethodInfo> _methodCache =
            new ConcurrentDictionary<MethodTypeKey, MethodInfo>();

        private static readonly ConcurrentDictionary<MultiMethodTypeKey, MethodInfo> _multiParamMethodCache =
            new ConcurrentDictionary<MultiMethodTypeKey, MethodInfo>();


        /// <summary>
        /// 获取多个泛型参数的泛型方法（缓存版本）
        /// </summary>
        public static MethodInfo MakeGenericMethodFast(this MethodInfo genericMethodDefinition, params Type[] typeArguments)
        {
            if (typeArguments.Length == 1)
            {
                var key = new MethodTypeKey(genericMethodDefinition, typeArguments[0]);
                return _methodCache.GetOrAdd(key, k => k.Method.MakeGenericMethod(k.Type));
            }
            else
            {
                var key = new MultiMethodTypeKey(genericMethodDefinition, typeArguments);
                return _multiParamMethodCache.GetOrAdd(key, k => k.Method.MakeGenericMethod(k.Types));
            }
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            _methodCache.Clear();
            _multiParamMethodCache.Clear();
        }
    }
}

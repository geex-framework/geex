using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions
{
    /// <summary>
    /// 高性能反射操作扩展方法
    /// 统一所有反射缓存和优化逻辑
    /// </summary>
    public static class ReflectionExtensions
    {
        #region 缓存键结构体 - 高性能无GC压力

        private readonly struct MethodTypeKey : IEquatable<MethodTypeKey>
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

        private readonly struct MemberKey : IEquatable<MemberKey>
        {
            public readonly MemberInfo Member;
            public readonly int HashCode;

            public MemberKey(MemberInfo member)
            {
                Member = member;
                HashCode = member.GetHashCode();
            }

            public bool Equals(MemberKey other) => Member == other.Member;
            public override bool Equals(object obj) => obj is MemberKey key && Equals(key);
            public override int GetHashCode() => HashCode;
        }

        private readonly struct TypeStringKey : IEquatable<TypeStringKey>
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

        #endregion

        #region 缓存存储

        // 泛型方法缓存
        private static readonly ConcurrentDictionary<MethodTypeKey, MethodInfo> _genericMethodCache = new();
        // 类型信息缓存
        private static readonly ConcurrentDictionary<Type, MethodInfo[]> _typeMethodsCache = new();
        private static readonly ConcurrentDictionary<TypeStringKey, PropertyInfo> _propertyInfoCache = new();

        #endregion

        #region MethodInfo 扩展方法

        /// <summary>
        /// 高性能泛型方法调用
        /// </summary>
        public static object GenericInvokeFast(this MethodInfo method, Type typeArgument, object target, params object[] args)
        {
            return method.GetGenericMethodCached(typeArgument).Invoke(target,args);
        }

        /// <summary>
        /// 获取泛型方法（缓存版本）
        /// </summary>
        public static MethodInfo GetGenericMethodCached(this MethodInfo method, Type typeArgument)
        {
            var key = new MethodTypeKey(method, typeArgument);
            return _genericMethodCache.GetOrAdd(key, k => k.Method.MakeGenericMethod(k.Type));
        }

        #endregion

        #region Type 扩展方法

        /// <summary>
        /// 高性能获取类型的非特殊方法
        /// </summary>
        public static MethodInfo[] GetNonSpecialMethodsCached(this Type type)
        {
            return _typeMethodsCache.GetOrAdd(type, t =>
                t.GetMethods().Where(x => !x.IsSpecialName).ToArray());
        }

        /// <summary>
        /// 高性能获取属性信息
        /// </summary>
        public static PropertyInfo GetPropertyCached(this Type type, string propertyName)
        {
            var key = new TypeStringKey(type, propertyName);
            return _propertyInfoCache.GetOrAdd(key, k => k.Type.GetProperty(k.Text));
        }

        #endregion

        #region 缓存管理

        /// <summary>
        /// 清理所有反射缓存
        /// </summary>
        public static void ClearReflectionCache()
        {
            _genericMethodCache.Clear();
            _typeMethodsCache.Clear();
            _propertyInfoCache.Clear();
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public static Dictionary<string, int> GetCacheStatistics()
        {
            return new Dictionary<string, int>
            {
                ["GenericMethodCache"] = _genericMethodCache.Count,
                ["TypeMethodsCache"] = _typeMethodsCache.Count,
                ["PropertyInfoCache"] = _propertyInfoCache.Count
            };
        }

        #endregion
    }
}

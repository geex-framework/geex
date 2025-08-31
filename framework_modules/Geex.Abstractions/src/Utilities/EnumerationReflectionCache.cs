using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

using FastExpressionCompiler;

namespace Geex.Utilities
{
    /// <summary>
    /// Enumeration类型反射操作的高性能缓存
    /// </summary>
    public static class EnumerationReflectionCache
    {
        /// <summary>
        /// FromValue方法的编译委托缓存
        /// Key: Enumeration类型
        /// Value: 编译的FromValue委托 (string value) => TEnum
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Func<string, object>> _fromValueDelegateCache
            = new ConcurrentDictionary<Type, Func<string, object>>();

        /// <summary>
        /// 高性能的FromValue调用
        /// </summary>
        /// <typeparam name="TEnum">枚举类型</typeparam>
        /// <param name="value">值</param>
        /// <returns>枚举实例</returns>
        public static TEnum FromValue<TEnum>(string value) where TEnum : Enumeration<TEnum>
        {
            var enumType = typeof(TEnum);
            var fromValueDelegate = _fromValueDelegateCache.GetOrAdd(enumType, CreateFromValueDelegate);
            return (TEnum)fromValueDelegate(value);
        }

        /// <summary>
        /// 通用的FromValue调用（支持object返回类型）
        /// </summary>
        public static object FromValue(Type enumType, object value)
        {
            var fromValueDelegate = _fromValueDelegateCache.GetOrAdd(enumType, CreateFromValueDelegate);
            return fromValueDelegate(value?.ToString());
        }

        /// <summary>
        /// 创建编译的FromValue委托
        /// </summary>
        private static Func<string, object> CreateFromValueDelegate(Type enumType)
        {
            // 获取静态FromValue方法
            var enumerationType = typeof(Enumeration<>).MakeGenericType(enumType);
            var fromValueMethod = enumerationType.GetMethod(
                nameof(Enumeration.FromValue),
                genericParameterCount: 1,
                types: new[] { typeof(string) });

            var genericFromValueMethod = fromValueMethod.MakeGenericMethod(enumType);

            // 创建表达式：(string value) => EnumerationType.FromValue<TEnum>(value)
            var valueParam = Expression.Parameter(typeof(string), "value");
            var methodCall = Expression.Call(genericFromValueMethod, valueParam);
            var boxedResult = Expression.Convert(methodCall, typeof(object));

            var lambda = Expression.Lambda<Func<string, object>>(boxedResult, valueParam);
            return lambda.CompileFast();
        }


        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            _fromValueDelegateCache.Clear();
        }
    }
}

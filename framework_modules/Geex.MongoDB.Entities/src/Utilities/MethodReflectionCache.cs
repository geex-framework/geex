using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using FastExpressionCompiler;

namespace MongoDB.Entities.Utilities
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

        private static readonly ConcurrentDictionary<MethodTypeKey, Func<object, object[], object>> _compiledInvokerCache =
            new ConcurrentDictionary<MethodTypeKey, Func<object, object[], object>>();

        private static readonly ConcurrentDictionary<MultiMethodTypeKey, MethodInfo> _multiParamMethodCache =
            new ConcurrentDictionary<MultiMethodTypeKey, MethodInfo>();

        /// <summary>
        /// 获取泛型方法（缓存版本）
        /// </summary>
        public static MethodInfo GetGenericMethod(MethodInfo genericMethodDefinition, Type typeArgument)
        {
            var key = new MethodTypeKey(genericMethodDefinition, typeArgument);
            return _methodCache.GetOrAdd(key, k => k.Method.MakeGenericMethod(k.Type));
        }

        /// <summary>
        /// 获取多个泛型参数的泛型方法（缓存版本）
        /// </summary>
        public static MethodInfo GetGenericMethod(MethodInfo genericMethodDefinition, params Type[] typeArguments)
        {
            if (typeArguments.Length == 1)
            {
                return GetGenericMethod(genericMethodDefinition, typeArguments[0]);
            }

            // 为多个泛型参数创建高性能键，避免字符串拼接
            var key = new MultiMethodTypeKey(genericMethodDefinition, typeArguments);
            return _multiParamMethodCache.GetOrAdd(key, k => k.Method.MakeGenericMethod(k.Types));
        }

        /// <summary>
        /// 获取编译的方法调用器
        /// </summary>
        public static Func<object, object[], object> GetCompiledInvoker(MethodInfo genericMethodDefinition, Type typeArgument)
        {
            var key = new MethodTypeKey(genericMethodDefinition, typeArgument);
            return _compiledInvokerCache.GetOrAdd(key, CreateCompiledInvoker);
        }

        private static Func<object, object[], object> CreateCompiledInvoker(MethodTypeKey key)
        {
            var method = GetGenericMethod(key.Method, key.Type);

            try
            {
                // 创建编译的调用器
                var targetParam = Expression.Parameter(typeof(object), "target");
                var argsParam = Expression.Parameter(typeof(object[]), "args");

                var parameters = method.GetParameters();
                var argExpressions = new Expression[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    var argAccess = Expression.ArrayIndex(argsParam, Expression.Constant(i));
                    argExpressions[i] = Expression.Convert(argAccess, paramType);
                }

                Expression callExpression;
                if (method.IsStatic)
                {
                    callExpression = Expression.Call(method, argExpressions);
                }
                else
                {
                    var targetCast = Expression.Convert(targetParam, method.DeclaringType);
                    callExpression = Expression.Call(targetCast, method, argExpressions);
                }

                Expression resultExpression;
                if (method.ReturnType == typeof(void))
                {
                    resultExpression = Expression.Block(callExpression, Expression.Constant(null));
                }
                else
                {
                    resultExpression = Expression.Convert(callExpression, typeof(object));
                }

                var lambda = Expression.Lambda<Func<object, object[], object>>(
                    resultExpression,
                    targetParam,
                    argsParam);

                return lambda.CompileFast();
            }
            catch (Exception)
            {
                // 如果编译失败，回退到反射调用
                return (target, args) => method.Invoke(target, args);
            }
        }

        /// <summary>
        /// 调用泛型方法（优化版本）
        /// </summary>
        public static object InvokeGenericMethod(MethodInfo genericMethodDefinition, Type typeArgument, object target, params object[] args)
        {
            var invoker = GetCompiledInvoker(genericMethodDefinition, typeArgument);
            return invoker(target, args);
        }

        /// <summary>
        /// 调用静态泛型方法（优化版本）
        /// </summary>
        public static object InvokeStaticGenericMethod(MethodInfo genericMethodDefinition, Type typeArgument, params object[] args)
        {
            return InvokeGenericMethod(genericMethodDefinition, typeArgument, null, args);
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            _methodCache.Clear();
            _multiParamMethodCache.Clear();
            _compiledInvokerCache.Clear();
        }
    }

    /// <summary>
    /// 强类型的泛型方法调用器
    /// </summary>
    public static class GenericMethodCache<TDelegate> where TDelegate : Delegate
    {
        private static readonly ConcurrentDictionary<MethodTypeKey, TDelegate> _delegateCache =
            new ConcurrentDictionary<MethodTypeKey, TDelegate>();

        /// <summary>
        /// 获取强类型的方法委托
        /// </summary>
        public static TDelegate GetDelegate(MethodInfo genericMethodDefinition, Type typeArgument)
        {
            var key = new MethodTypeKey(genericMethodDefinition, typeArgument);
            return _delegateCache.GetOrAdd(key, CreateDelegate);
        }

        private static TDelegate CreateDelegate(MethodTypeKey key)
        {
            var method = MethodReflectionCache.GetGenericMethod(key.Method, key.Type);

            try
            {
                return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), method);
            }
            catch (Exception)
            {
                // 如果创建委托失败，返回null
                return null;
            }
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            _delegateCache.Clear();
        }
    }
}

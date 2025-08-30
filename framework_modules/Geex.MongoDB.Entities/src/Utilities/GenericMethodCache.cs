using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

using FastExpressionCompiler;

namespace MongoDB.Entities.Utilities
{
    /// <summary>
    /// 泛型方法调用缓存，用于优化高频的泛型方法调用
    /// </summary>
    public static class GenericMethodCache
    {
        private static readonly ConcurrentDictionary<(MethodInfo, Type), MethodInfo> _methodCache = 
            new ConcurrentDictionary<(MethodInfo, Type), MethodInfo>();

        private static readonly ConcurrentDictionary<(MethodInfo, Type), Func<object, object[], object>> _compiledInvokerCache =
            new ConcurrentDictionary<(MethodInfo, Type), Func<object, object[], object>>();

        /// <summary>
        /// 获取泛型方法（缓存版本）
        /// </summary>
        public static MethodInfo GetGenericMethod(MethodInfo genericMethodDefinition, Type typeArgument)
        {
            var key = (genericMethodDefinition, typeArgument);
            return _methodCache.GetOrAdd(key, k => k.Item1.MakeGenericMethod(k.Item2));
        }

        /// <summary>
        /// 获取编译的方法调用器
        /// </summary>
        public static Func<object, object[], object> GetCompiledInvoker(MethodInfo genericMethodDefinition, Type typeArgument)
        {
            var key = (genericMethodDefinition, typeArgument);
            return _compiledInvokerCache.GetOrAdd(key, CreateCompiledInvoker);
        }

        private static Func<object, object[], object> CreateCompiledInvoker((MethodInfo, Type) key)
        {
            var (genericMethodDefinition, typeArgument) = key;
            var method = GetGenericMethod(genericMethodDefinition, typeArgument);

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
            _compiledInvokerCache.Clear();
        }
    }

    /// <summary>
    /// 强类型的泛型方法调用器
    /// </summary>
    public static class GenericMethodCache<TDelegate> where TDelegate : Delegate
    {
        private static readonly ConcurrentDictionary<(MethodInfo, Type), TDelegate> _delegateCache =
            new ConcurrentDictionary<(MethodInfo, Type), TDelegate>();

        /// <summary>
        /// 获取强类型的方法委托
        /// </summary>
        public static TDelegate GetDelegate(MethodInfo genericMethodDefinition, Type typeArgument)
        {
            var key = (genericMethodDefinition, typeArgument);
            return _delegateCache.GetOrAdd(key, CreateDelegate);
        }

        private static TDelegate CreateDelegate((MethodInfo, Type) key)
        {
            var (genericMethodDefinition, typeArgument) = key;
            var method = GenericMethodCache.GetGenericMethod(genericMethodDefinition, typeArgument);
            
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

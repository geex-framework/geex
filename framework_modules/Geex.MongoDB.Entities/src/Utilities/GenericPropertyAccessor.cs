using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities.Utilities
{
    /// <summary>
    /// 泛型属性访问器，用于高性能的泛型类型属性访问
    /// </summary>
    public static class GenericPropertyAccessor
    {
        /// <summary>
        /// 泛型类型属性访问器缓存
        /// Key: (泛型定义类型, 泛型参数类型, 属性名)
        /// Value: 编译的属性访问委托
        /// </summary>
        private static readonly ConcurrentDictionary<(Type, Type, string), Func<object, object>>
            _propertyAccessorCache = new ConcurrentDictionary<(Type, Type, string), Func<object, object>>();

        /// <summary>
        /// 泛型类型信息缓存
        /// Key: (泛型定义类型, 泛型参数类型)
        /// Value: 构造的泛型类型
        /// </summary>
        private static readonly ConcurrentDictionary<(Type, Type), Type>
            _genericTypeCache = new ConcurrentDictionary<(Type, Type), Type>();

        /// <summary>
        /// 属性信息缓存
        /// Key: (类型, 属性名)
        /// Value: PropertyInfo
        /// </summary>
        private static readonly ConcurrentDictionary<(Type, string), PropertyInfo>
            _propertyInfoCache = new ConcurrentDictionary<(Type, string), PropertyInfo>();

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
            var key = (genericTypeDefinition, typeArgument, propertyName);
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
            var key = (genericTypeDefinition, typeArgument);
            return _genericTypeCache.GetOrAdd(key, k => k.Item1.MakeGenericType(k.Item2));
        }

        /// <summary>
        /// 获取属性信息（缓存版本）
        /// </summary>
        public static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            var key = (type, propertyName);
            return _propertyInfoCache.GetOrAdd(key, k => k.Item1.GetProperty(k.Item2));
        }

        /// <summary>
        /// 创建编译的属性访问器
        /// </summary>
        private static Func<object, object> CreatePropertyAccessor((Type, Type, string) key)
        {
            var (genericTypeDefinition, typeArgument, propertyName) = key;

            try
            {
                // 获取具体的泛型类型
                var concreteType = GetGenericType(genericTypeDefinition, typeArgument);
                var propertyInfo = GetPropertyInfo(concreteType, propertyName);

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
            catch (Exception)
            {
                // 如果编译失败，回退到反射方式
                return CreateReflectionBasedAccessor(key);
            }
        }

        /// <summary>
        /// 创建基于反射的属性访问器（回退方案）
        /// </summary>
        private static Func<object, object> CreateReflectionBasedAccessor((Type, Type, string) key)
        {
            var (genericTypeDefinition, typeArgument, propertyName) = key;

            return instance =>
            {
                try
                {
                    var concreteType = GetGenericType(genericTypeDefinition, typeArgument);
                    var propertyInfo = GetPropertyInfo(concreteType, propertyName);
                    return propertyInfo?.GetValue(instance);
                }
                catch
                {
                    return null;
                }
            };
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

    /// <summary>
    /// 针对ExpressionDataFilter的专门优化
    /// </summary>
    public static class ExpressionDataFilterAccessor
    {
        private static readonly Type ExpressionDataFilterType = typeof(ExpressionDataFilter<>);

        /// <summary>
        /// 高性能获取PreFilterExpression属性值
        /// </summary>
        public static LambdaExpression GetPreFilterExpression(Type targetType, object filterInstance)
        {
            return GenericPropertyAccessor.GetGenericPropertyValue<LambdaExpression>(
                ExpressionDataFilterType, targetType, nameof(ExpressionDataFilter<object>.PreFilterExpression), filterInstance);
        }

        /// <summary>
        /// 高性能获取PostFilterExpression属性值
        /// </summary>
        public static LambdaExpression GetPostFilterExpression(Type targetType, object filterInstance)
        {
            return GenericPropertyAccessor.GetGenericPropertyValue<LambdaExpression>(
                ExpressionDataFilterType, targetType, nameof(ExpressionDataFilter<object>.PostFilterExpression), filterInstance);
        }
    }
}

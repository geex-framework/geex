using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace Geex.Utilities
{
    /// <summary>
    /// HotChocolate类型系统反射操作的高性能缓存
    /// </summary>
    public static class HotChocolateReflectionCache
    {
        /// <summary>
        /// 类型方法信息缓存
        /// Key: 类型
        /// Value: 非特殊名称的方法列表
        /// </summary>
        private static readonly ConcurrentDictionary<Type, MethodInfo[]> _typeMethodsCache
            = new ConcurrentDictionary<Type, MethodInfo[]>();

        /// <summary>
        /// ObjectTypeExtension属性信息缓存
        /// Key: 属性名
        /// Value: PropertyInfo
        /// </summary>
        private static readonly ConcurrentDictionary<string, PropertyInfo> _extensionPropertiesCache
            = new ConcurrentDictionary<string, PropertyInfo>();

        /// <summary>
        /// ObjectTypeDescriptor.Fields属性访问器缓存
        /// </summary>
        private static readonly Lazy<Func<object, object>> _getFieldsAccessor = new Lazy<Func<object, object>>(CreateGetFieldsAccessor);

        /// <summary>
        /// 高性能获取类型的非特殊名称方法
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>方法信息数组</returns>
        public static MethodInfo[] GetNonSpecialMethods(Type type)
        {
            return _typeMethodsCache.GetOrAdd(type, t => 
                t.GetMethods().Where(x => !x.IsSpecialName).ToArray());
        }

        /// <summary>
        /// 高性能获取ObjectTypeExtension属性
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="propertyName">属性名</param>
        /// <returns>PropertyInfo</returns>
        public static PropertyInfo GetObjectTypeExtensionProperty(Type type, string propertyName)
        {
            // 由于ObjectTypeExtension的属性是固定的，我们可以缓存它们
            return _extensionPropertiesCache.GetOrAdd(propertyName, name => type.GetProperty(name));
        }

        /// <summary>
        /// 高性能获取ObjectTypeDescriptor的Fields属性值
        /// </summary>
        /// <typeparam name="T">描述符类型</typeparam>
        /// <param name="descriptor">描述符实例</param>
        /// <returns>字段描述符集合</returns>
        public static ICollection<ObjectFieldDescriptor> GetFields<T>(IObjectTypeDescriptor<T> descriptor)
        {
            var fieldsAccessor = _getFieldsAccessor.Value;
            return fieldsAccessor(descriptor) as ICollection<ObjectFieldDescriptor>;
        }

        /// <summary>
        /// 创建编译的Fields属性访问器
        /// </summary>
        private static Func<object, object> CreateGetFieldsAccessor()
        {
            try
            {
                // 获取ObjectTypeDescriptor类型和Fields属性
                var descriptorType = typeof(ObjectTypeDescriptor);
                var fieldsProperty = descriptorType.GetProperty("Fields", BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (fieldsProperty != null)
                {
                    // 创建表达式：(object descriptor) => ((ObjectTypeDescriptor)descriptor).Fields
                    var descriptorParam = Expression.Parameter(typeof(object), "descriptor");
                    var typedDescriptor = Expression.Convert(descriptorParam, descriptorType);
                    var propertyAccess = Expression.Property(typedDescriptor, fieldsProperty);
                    var boxedResult = Expression.Convert(propertyAccess, typeof(object));
                    
                    var lambda = Expression.Lambda<Func<object, object>>(boxedResult, descriptorParam);
                    return lambda.CompileFast();
                }
            }
            catch (Exception)
            {
                // 编译失败，回退到反射方式
            }

            // 回退到反射访问
            return CreateReflectionBasedFieldsAccessor();
        }

        /// <summary>
        /// 创建基于反射的Fields属性访问器（回退方案）
        /// </summary>
        private static Func<object, object> CreateReflectionBasedFieldsAccessor()
        {
            var descriptorType = typeof(ObjectTypeDescriptor);
            var fieldsProperty = descriptorType.GetProperty("Fields", BindingFlags.NonPublic | BindingFlags.Instance);
            
            return descriptor =>
            {
                try
                {
                    return fieldsProperty?.GetValue(descriptor);
                }
                catch
                {
                    return null;
                }
            };
        }

        /// <summary>
        /// 批量获取ObjectTypeExtension属性（用于IgnoreExtensionFields）
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>常用扩展属性的PropertyInfo字典</returns>
        public static Dictionary<string, PropertyInfo> GetExtensionProperties(Type type)
        {
            var propertyNames = new[] 
            {
                nameof(ObjectTypeExtension.Kind),
                nameof(ObjectTypeExtension.Scope), 
                nameof(ObjectTypeExtension.Name),
                nameof(ObjectTypeExtension.Description),
                nameof(ObjectTypeExtension.ContextData)
            };

            var properties = new Dictionary<string, PropertyInfo>();
            foreach (var propertyName in propertyNames)
            {
                var property = GetObjectTypeExtensionProperty(type, propertyName);
                if (property != null)
                {
                    properties[propertyName] = property;
                }
            }
            
            return properties;
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearCache()
        {
            _typeMethodsCache.Clear();
            _extensionPropertiesCache.Clear();
        }
    }
}

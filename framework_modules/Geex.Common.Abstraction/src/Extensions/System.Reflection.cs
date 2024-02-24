using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace System
{
    public static class SystemTypeExtensions
    {
        public static bool IsDynamic(this Type type) => typeof(IDynamicMetaObjectProvider).IsAssignableFrom(type);
        public static List<TPropertyType> GetPropertiesOfType<TPropertyType>(this Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(p => p.PropertyType.IsAssignableTo(type))
                .Select(pi => (TPropertyType)pi.GetValue(null))
                .ToList();
        }
        private static Dictionary<Type, PropertyInfo[]> _typeCache = new();
        /// <summary>
        /// Create a dictionary from the given object (<paramref name="obj"/>).
        /// </summary>
        /// <param name="obj">Source object.</param>
        /// <returns>Created dictionary.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null.</exception>
        public static IDictionary<string, object> ToDictionary(this object obj)
        {

            if (obj == null)
                throw new ArgumentNullException("obj");

            if (obj is IDictionary<string, object> already)
            {
                return already;
            }

            var type = obj.GetType();
            if (!_typeCache.TryGetValue(type, out var props))
            {
                props = type.GetProperties();
            }

            return props.ToDictionary(x => x.Name, x => x.GetValue(obj, null));
        }

        public static bool IsAutoProperty(this PropertyInfo prop)
        {
            if (!prop.CanWrite || !prop.CanRead)
                return false;

            return prop.DeclaringType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                                     .Any(f => f.Name.Contains("<" + prop.Name + ">"));
        }
        /// <summary>
        /// 获取领域名称, bug:需要加缓存以优化性能
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DomainName(this Type value)
        {
            return value.Namespace.Split(".").ToList().Last(x => !x.IsIn("Gql", "Api", "Core", "Tests")).ToCamelCase();
        }
        public static bool ImplementsOrInherits<TFrom>(this Type @this)
        {
            return @this.ImplementsOrInherits(typeof(TFrom));
        }

        public static bool ImplementsOrInherits(this Type @this, Type from)
        {
            if (from is null)
            {
                return false;
            }
            else if (!from.IsGenericType)
            {
                return from.IsAssignableFrom(@this);
            }
            else if (!from.IsGenericTypeDefinition)
            {
                return from.IsAssignableFrom(@this);
            }
            else if (from.IsInterface)
            {
                foreach (Type @interface in @this.GetInterfaces())
                {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == from)
                    {
                        return true;
                    }
                }
            }

            if (@this.IsGenericType && @this.GetGenericTypeDefinition() == from)
            {
                return true;
            }

            return @this.BaseType?.ImplementsOrInherits(from) ?? false;
        }
    }

}

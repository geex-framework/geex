using System;
using System.Linq;
using System.Reflection;

using MongoDB.Entities;

namespace MongoDB.Entities.Utilities
{
    public static class BatchLoadNavigationValidator
    {
        public static bool TryValidate(PropertyInfo property, Type entityType, out string? error)
        {
            error = null;
            if (property == null)
            {
                error = "property 不能为空";
                return false;
            }

            if (entityType == null)
            {
                error = "entityType 不能为空";
                return false;
            }

            if (!IsLazyNavigationPropertyType(property.PropertyType))
            {
                error = $"属性类型 '{property.PropertyType.Name}' 不是 IQueryable<> 或 Lazy<>";
                return false;
            }

            if (!LazyQueryMetadataRegistry.IsRegistered(entityType, property.Name))
            {
                error = "未通过 ConfigLazyQuery 注册 LazyQuery";
                return false;
            }

            return true;
        }

        public static void Ensure(PropertyInfo property, Type entityType)
        {
            if (!TryValidate(property, entityType, out var error))
            {
                throw BatchLoadException.NavigationNotBatchable(property, entityType, error!);
            }
        }

        private static bool IsLazyNavigationPropertyType(Type propertyType)
        {
            if (propertyType.IsGenericType)
            {
                var genericDefinition = propertyType.GetGenericTypeDefinition();
                if (genericDefinition == typeof(IQueryable<>) || genericDefinition == typeof(Lazy<>))
                {
                    return typeof(IEntityBase).IsAssignableFrom(propertyType.GenericTypeArguments[0]);
                }
            }

            if (propertyType.Name is "Lazy`1" or "ResettableLazy`1" &&
                propertyType.GenericTypeArguments.Length > 0)
            {
                return typeof(IEntityBase).IsAssignableFrom(propertyType.GenericTypeArguments[0]);
            }

            return false;
        }
    }
}

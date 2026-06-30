using System;
using System.Linq;
using System.Reflection;

using MongoDB.Entities;

namespace MongoDB.Entities.Utilities
{
    public static class BatchLoadPropertyExtensions
    {
        public static bool TryValidateBatchLoadable(
            this PropertyInfo property,
            Type entityType,
            out string? error)
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

            if (!property.PropertyType.IsLazyEntityNavigation())
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

        public static void EnsureBatchLoadable(this PropertyInfo property, Type entityType)
        {
            if (!property.TryValidateBatchLoadable(entityType, out var error))
            {
                throw BatchLoadException.NavigationNotBatchable(property, entityType, error!);
            }
        }

        public static bool IsLazyEntityNavigation(this Type propertyType)
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

        public static bool TryGetRelatedEntityType(
            this PropertyInfo property,
            out Type relatedEntityType)
        {
            relatedEntityType = null!;
            if (!property.PropertyType.IsLazyEntityNavigation())
            {
                return false;
            }

            if (property.PropertyType.IsGenericType)
            {
                relatedEntityType = property.PropertyType.GenericTypeArguments[0];
                return typeof(IEntityBase).IsAssignableFrom(relatedEntityType);
            }

            if (property.PropertyType.Name is "Lazy`1" or "ResettableLazy`1" &&
                property.PropertyType.GenericTypeArguments.Length > 0)
            {
                relatedEntityType = property.PropertyType.GenericTypeArguments[0];
                return typeof(IEntityBase).IsAssignableFrom(relatedEntityType);
            }

            return false;
        }

        public static PropertyInfo? ResolveBatchLoadProperty(
            this Type declaringEntityType,
            string propertyName)
        {
            for (var current = declaringEntityType; current != null; current = current.BaseType)
            {
                var property = current.GetProperty(
                    propertyName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);
                if (property != null && property.PropertyType.IsLazyEntityNavigation())
                {
                    return property;
                }

                property = current.GetProperty(
                    propertyName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (property != null && property.PropertyType.IsLazyEntityNavigation())
                {
                    return property;
                }
            }

            return null;
        }
    }
}

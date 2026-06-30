using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HotChocolate;
using HotChocolate.Types;

using MongoDB.Entities;
using MongoDB.Entities.Utilities;

namespace Geex.Gql.AutoBatchLoad
{
    internal static class LazyNavigationMapper
    {
        private static readonly HashSet<string> LazyTypeNames = new(StringComparer.Ordinal)
        {
            "Lazy`1",
            "ResettableLazy`1",
            "IQueryable`1"
        };

        public static PropertyInfo? ResolveNavigationProperty(Type entityType, IOutputField field)
        {
            if (field is IObjectField objectField)
            {
                if (objectField.ResolverMember is PropertyInfo resolverProperty &&
                    IsLazyQueryNavigation(entityType, resolverProperty))
                {
                    return resolverProperty;
                }

                if (objectField.Member is PropertyInfo memberProperty &&
                    IsLazyQueryNavigation(entityType, memberProperty))
                {
                    return memberProperty;
                }
            }

            return FindPropertyByName(entityType, field.Name);
        }

        public static bool IsLazyQueryNavigation(Type entityType, PropertyInfo property) =>
            IsLazyNavigationPropertyType(property.PropertyType) &&
            LazyQueryMetadataRegistry.IsRegistered(entityType, property.Name);

        public static bool TryGetRelatedEntityType(PropertyInfo property, out Type relatedEntityType)
        {
            relatedEntityType = null!;
            var propertyType = property.PropertyType;

            if (propertyType.IsGenericType)
            {
                var genericDefinition = propertyType.GetGenericTypeDefinition();
                if (genericDefinition == typeof(IQueryable<>) || genericDefinition == typeof(Lazy<>))
                {
                    relatedEntityType = propertyType.GenericTypeArguments[0];
                    return typeof(IEntityBase).IsAssignableFrom(relatedEntityType);
                }
            }

            if (propertyType.Name is "Lazy`1" or "ResettableLazy`1" &&
                propertyType.GenericTypeArguments.Length > 0)
            {
                relatedEntityType = propertyType.GenericTypeArguments[0];
                return typeof(IEntityBase).IsAssignableFrom(relatedEntityType);
            }

            return false;
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

            return LazyTypeNames.Contains(propertyType.Name) &&
                   propertyType.GenericTypeArguments.Length > 0 &&
                   typeof(IEntityBase).IsAssignableFrom(propertyType.GenericTypeArguments[0]);
        }

        private static PropertyInfo? FindPropertyByName(Type entityType, string fieldName)
        {
            var currentType = entityType;
            while (currentType != null)
            {
                foreach (var property in currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                {
                    if (string.Equals(property.Name, fieldName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(ToCamelCase(property.Name), fieldName, StringComparison.Ordinal))
                    {
                        if (IsLazyQueryNavigation(entityType, property))
                        {
                            return property;
                        }
                    }
                }

                currentType = currentType.BaseType;
            }

            return null;
        }

        private static string ToCamelCase(string name) =>
            name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;

using HotChocolate.Types;

using MongoDB.Entities;
using MongoDB.Entities.Utilities;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public static class OutputFieldExtensions
{
    public static bool TryGetReturningEntityType(this IOutputField field, out Type entityType)
    {
        if (TryGetEntityElementTypeFromOutputField(field, out entityType))
        {
            return true;
        }

        if (field is IObjectField objectField)
        {
            foreach (var returnType in GetRuntimeReturnTypes(objectField))
            {
                if (returnType.TryGetEntityReturningKind(out var kind, out entityType) &&
                    (kind & EntityReturningKind.Observable) != 0)
                {
                    return true;
                }
            }
        }

        entityType = null!;
        return false;
    }

    public static bool TryGetObservablePayloadType(this IOutputField field, out Type payloadType)
    {
        payloadType = null!;

        if (field is IObjectField objectField)
        {
            foreach (var returnType in GetRuntimeReturnTypes(objectField))
            {
                if (returnType.TryGetObservablePayloadType(out payloadType))
                {
                    return true;
                }
            }
        }

        return field.Type.ToRuntimeType().TryGetObservablePayloadType(out payloadType);
    }

    public static PropertyInfo? ResolveNavigationProperty(this IOutputField field, Type entityType)
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

        return FindNavigationPropertyByName(entityType, field.Name);
    }

    public static bool IsOffsetPagingField(this IOutputField field) =>
        field.Type.NamedType().Name.EndsWith("CollectionSegment", StringComparison.Ordinal);

    private static bool TryGetEntityElementTypeFromOutputField(IOutputField field, out Type entityType)
    {
        entityType = null!;

        if (field is IObjectField objectField)
        {
            if (objectField.Member is MethodInfo memberMethod &&
                memberMethod.ReturnType.TryGetEntityReturningKind(out _, out entityType))
            {
                return true;
            }

            if (objectField.ResolverMember is MethodInfo resolverMethod &&
                resolverMethod.ReturnType.TryGetEntityReturningKind(out _, out entityType))
            {
                return true;
            }
        }

        if (field.Type.ToRuntimeType().TryGetEntityReturningKind(out _, out entityType))
        {
            return true;
        }

        if (field.Type.IsListType())
        {
            var elementRuntimeType = field.Type.ElementType().ToRuntimeType();
            if (IsEntityType(elementRuntimeType))
            {
                entityType = elementRuntimeType;
                return true;
            }
        }

        var namedType = field.Type.NamedType();
        if (namedType is IObjectType objectType &&
            IsEntityType(objectType.RuntimeType))
        {
            entityType = objectType.RuntimeType;
            return true;
        }

        return false;
    }

    private static IEnumerable<Type?> GetRuntimeReturnTypes(IObjectField objectField)
    {
        if (objectField.Member is MethodInfo memberMethod)
        {
            yield return memberMethod.ReturnType;
        }

        if (objectField.ResolverMember is MethodInfo resolverMethod)
        {
            yield return resolverMethod.ReturnType;
        }
    }

    private static bool IsLazyQueryNavigation(Type entityType, PropertyInfo property) =>
        property.PropertyType.IsLazyEntityNavigation() &&
        LazyQueryMetadataRegistry.IsRegistered(entityType, property.Name);

    private static PropertyInfo? FindNavigationPropertyByName(Type entityType, string fieldName)
    {
        var currentType = entityType;
        while (currentType != null)
        {
            foreach (var property in currentType.GetProperties(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
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

    private static bool IsEntityType(Type? type) =>
        type != null && typeof(IEntityBase).IsAssignableFrom(type);

    private static string ToCamelCase(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
}

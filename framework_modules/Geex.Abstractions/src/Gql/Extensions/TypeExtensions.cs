using System;
using System.Collections;
using System.Collections.Generic;

using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

using MongoDB.Entities;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public static class TypeExtensions
{
    public static Type ResolveNavigationEntityType(this Type entityType, IMiddlewareContext context)
    {
        if (!entityType.IsInterface)
        {
            return entityType;
        }

        return entityType.TryResolveEntityObjectType(context, out var objectType)
            ? objectType.RuntimeType
            : entityType;
    }

    public static bool TryResolveEntityObjectType(
        this Type entityType,
        IMiddlewareContext context,
        out IObjectType objectType)
    {
        foreach (var typeName in GetGraphQLTypeNameCandidates(entityType))
        {
            if (context.Schema.GetType<IObjectType>(typeName) is { } resolvedType)
            {
                objectType = resolvedType;
                return true;
            }
        }

        objectType = null!;
        return false;
    }

    public static bool TryGetEntityElementType(this Type? type, out Type entityType)
    {
        entityType = null!;
        type = type.UnwrapAsyncReturnType();
        if (type == null)
        {
            return false;
        }

        if (type.IsGenericType)
        {
            var genericDefinition = type.GetGenericTypeDefinition();
            if (genericDefinition == typeof(IQueryable<>))
            {
                entityType = type.GetGenericArguments()[0];
                return IsEntityType(entityType);
            }

            if (genericDefinition == typeof(CollectionSegment<>))
            {
                entityType = type.GetGenericArguments()[0];
                return IsEntityType(entityType);
            }

            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                entityType = type.GetGenericArguments()[0];
                return IsEntityType(entityType);
            }
        }

        if (type.IsArray && IsEntityType(type.GetElementType()))
        {
            entityType = type.GetElementType()!;
            return true;
        }

        return false;
    }

    public static bool TryGetObservableEntityElementType(this Type? type, out Type entityType)
    {
        entityType = null!;
        type = type.UnwrapAsyncReturnType();
        if (type == null)
        {
            return false;
        }

        if (type.IsGenericType)
        {
            var genericDefinition = type.GetGenericTypeDefinition();
            if (genericDefinition == typeof(IObservable<>))
            {
                return type.GetGenericArguments()[0].TryGetEntityElementType(out entityType);
            }
        }

        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IObservable<>))
            {
                return iface.GetGenericArguments()[0].TryGetEntityElementType(out entityType);
            }
        }

        return false;
    }

    private static IEnumerable<string> GetGraphQLTypeNameCandidates(Type entityType)
    {
        var typeName = entityType.Name;
        if (entityType.IsInterface &&
            typeName.StartsWith('I') &&
            typeName.Length > 1 &&
            char.IsUpper(typeName[1]))
        {
            yield return typeName[1..];
        }

        yield return typeName;
    }

    private static bool IsEntityType(Type? type) =>
        type != null && typeof(IEntityBase).IsAssignableFrom(type);
}

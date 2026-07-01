using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

using MongoDB.Entities;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types
{
    [Flags]
    public enum EntityReturningKind
    {
        None = 0,
        Queryable = 1,
        Observable = 2,
        All = Queryable | Observable,
    }

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

        public static bool TryGetEntityReturningKind(
            this Type? type,
            out EntityReturningKind kind,
            out Type entityType)
        {
            kind = EntityReturningKind.None;
            entityType = null!;
            type = type.UnwrapAsyncReturnType();
            if (type == null)
            {
                return false;
            }

            if (TryGetObservableInnerType(type, out var innerType))
            {
                innerType = innerType.UnwrapAsyncReturnType();
                if (TryGetQueryableEntityElement(innerType, out entityType))
                {
                    kind = EntityReturningKind.Queryable | EntityReturningKind.Observable;
                    return true;
                }

                return false;
            }

            if (TryGetQueryableEntityElement(type, out entityType))
            {
                kind = EntityReturningKind.Queryable;
                return true;
            }

            return false;
        }

        private static bool TryGetObservableInnerType(Type type, out Type innerType)
        {
            innerType = null!;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IObservable<>))
            {
                innerType = type.GetGenericArguments()[0];
                return true;
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IObservable<>))
                {
                    innerType = iface.GetGenericArguments()[0];
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetQueryableEntityElement(Type? type, out Type entityType)
        {
            entityType = null!;
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
}

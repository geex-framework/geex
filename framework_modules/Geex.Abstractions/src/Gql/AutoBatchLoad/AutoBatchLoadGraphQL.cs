using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Geex.Gql.Types;

using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Pagination;

using MongoDB.Entities;
using MongoDB.Entities.Utilities;

namespace Geex.Gql.AutoBatchLoad
{
    /// <summary>
    /// GraphQL schema / selection 分析相关的 AutoBatchLoad 内部工具。
    /// </summary>
    internal static class AutoBatchLoadGraphQL
    {
        public static bool IsOperationObjectType(Type? runtimeType, string? typeName = null)
        {
            if (runtimeType == typeof(Query) ||
                runtimeType == typeof(Mutation) ||
                runtimeType == typeof(Subscription))
            {
                return true;
            }

            for (var current = runtimeType; current != null && current != typeof(object); current = current.BaseType)
            {
                if (!current.IsGenericType)
                {
                    continue;
                }

                var genericDefinition = current.GetGenericTypeDefinition();
                if (genericDefinition == typeof(QueryExtension<>) ||
                    genericDefinition == typeof(MutationExtension<>) ||
                    genericDefinition == typeof(SubscriptionExtension<>))
                {
                    return true;
                }
            }

            return typeName is OperationTypeNames.Query
                or OperationTypeNames.Mutation
                or OperationTypeNames.Subscription;
        }

        public static bool IsQueryableEntityRootField(ObjectFieldDefinition field) =>
            TryGetEntityElementType(field, out _);

        public static bool IsObservableEntityRootField(ObjectFieldDefinition field) =>
            TryGetObservableEntityElementType(field, out _);

        public static bool TryGetNavigationEntityType(IOutputField field, out Type entityType)
        {
            if (TryGetEntityElementTypeFromOutputField(field, out entityType))
            {
                return true;
            }

            if (field is IObjectField objectField)
            {
                foreach (var returnType in GetRuntimeReturnTypes(objectField))
                {
                    if (TryGetObservableEntityElementType(returnType, out entityType))
                    {
                        return true;
                    }
                }
            }

            entityType = null!;
            return false;
        }

        public static Type ResolveNavigationEntityType(IMiddlewareContext context, Type entityType)
        {
            if (!entityType.IsInterface)
            {
                return entityType;
            }

            return TryResolveEntityObjectType(context, entityType, out var objectType)
                ? objectType.RuntimeType
                : entityType;
        }

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

            return FindNavigationPropertyByName(entityType, field.Name);
        }

        public static bool TryGetEntityElementType(ObjectFieldDefinition field, out Type entityType)
        {
            entityType = null!;

            if (TryGetEntityElementType(field.ResultType, out entityType))
            {
                return true;
            }

            if (field.ResolverMember is MethodInfo resolverMethod &&
                TryGetEntityElementType(resolverMethod.ReturnType, out entityType))
            {
                return true;
            }

            if (field.Member is MethodInfo memberMethod &&
                TryGetEntityElementType(memberMethod.ReturnType, out entityType))
            {
                return true;
            }

            return false;
        }

        public static bool TryGetObservableEntityElementType(ObjectFieldDefinition field, out Type entityType)
        {
            entityType = null!;

            foreach (var returnType in GetDeclaredReturnTypes(field))
            {
                if (TryGetObservableEntityElementType(returnType, out entityType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLazyQueryNavigation(Type entityType, PropertyInfo property) =>
            BatchLoadNavigationValidator.IsLazyNavigationPropertyType(property.PropertyType) &&
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

        public static bool TryResolveEntityObjectType(
            IMiddlewareContext context,
            Type entityType,
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

        private static bool TryGetEntityElementTypeFromOutputField(IOutputField field, out Type entityType)
        {
            entityType = null!;

            if (field is IObjectField objectField)
            {
                if (objectField.Member is MethodInfo memberMethod &&
                    TryGetEntityElementType(memberMethod.ReturnType, out entityType))
                {
                    return true;
                }

                if (objectField.ResolverMember is MethodInfo resolverMethod &&
                    TryGetEntityElementType(resolverMethod.ReturnType, out entityType))
                {
                    return true;
                }
            }

            if (TryGetEntityElementType(field.Type.ToRuntimeType(), out entityType))
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

        private static IEnumerable<Type?> GetDeclaredReturnTypes(ObjectFieldDefinition field)
        {
            yield return field.ResultType;
            if (field.ResolverMember is MethodInfo resolverMethod)
            {
                yield return resolverMethod.ReturnType;
            }

            if (field.Member is MethodInfo memberMethod)
            {
                yield return memberMethod.ReturnType;
            }
        }

        private static bool TryGetObservableEntityElementType(Type? type, out Type entityType)
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
                    return TryGetEntityElementType(type.GetGenericArguments()[0], out entityType);
                }
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IObservable<>))
                {
                    return TryGetEntityElementType(iface.GetGenericArguments()[0], out entityType);
                }
            }

            return false;
        }

        private static bool TryGetEntityElementType(Type? type, out Type entityType)
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

        private static bool IsEntityType(Type? type) =>
            type != null && typeof(IEntityBase).IsAssignableFrom(type);

        private static string ToCamelCase(string name) =>
            name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
    }
}

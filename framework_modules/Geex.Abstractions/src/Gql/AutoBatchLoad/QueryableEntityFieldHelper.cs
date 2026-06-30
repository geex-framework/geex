using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Pagination;

using MongoDB.Entities;

namespace Geex.Gql.AutoBatchLoad
{
    internal static class QueryableEntityFieldHelper
    {
        public static bool IsQueryableEntityRootField(ObjectFieldDefinition field) =>
            TryGetEntityElementType(field, out _);

        public static bool IsObservableEntityRootField(ObjectFieldDefinition field) =>
            TryGetObservableEntityElementType(field, out _);

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

        public static bool TryGetEntityElementType(IOutputField field, out Type entityType) =>
            TryGetNavigationEntityType(field, out entityType);

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
    }
}

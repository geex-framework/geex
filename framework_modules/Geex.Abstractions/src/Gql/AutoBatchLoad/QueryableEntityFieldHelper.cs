using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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

        public static bool TryGetEntityElementType(IOutputField field, out Type entityType)
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

        private static bool TryGetEntityElementType(Type? type, out Type entityType)
        {
            entityType = null!;
            type = UnwrapTask(type);
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

                if (genericDefinition == typeof(CollectionSegment<>) ||
                    genericDefinition.Name.EndsWith("CollectionSegment", StringComparison.Ordinal))
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

        private static Type? UnwrapTask(Type? type)
        {
            while (type != null &&
                   type.IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                type = type.GetGenericArguments()[0];
            }

            return type;
        }

        private static bool IsEntityType(Type? type) =>
            type != null && typeof(IEntityBase).IsAssignableFrom(type);
    }
}

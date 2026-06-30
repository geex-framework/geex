using System;
using System.Collections.Generic;
using System.Linq;

using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;

using MongoDB.Entities.Utilities;

namespace Geex.Gql.AutoBatchLoad
{
    internal static class EntityGraphQLTypeResolver
    {
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
    }
}

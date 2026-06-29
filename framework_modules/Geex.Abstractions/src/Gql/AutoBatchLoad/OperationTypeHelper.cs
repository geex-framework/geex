using System;

using Geex.Gql.Types;

using HotChocolate.Types;

namespace Geex.Gql.AutoBatchLoad
{
    internal static class OperationTypeHelper
    {
        public static bool IsOperationObjectType(Type? runtimeType, string? typeName = null)
        {
            if (IsOperationRootType(runtimeType))
            {
                return true;
            }

            if (IsOperationExtensionType(runtimeType))
            {
                return true;
            }

            return typeName is OperationTypeNames.Query
                or OperationTypeNames.Mutation
                or OperationTypeNames.Subscription;
        }

        private static bool IsOperationRootType(Type? runtimeType) =>
            runtimeType == typeof(Query) ||
            runtimeType == typeof(Mutation) ||
            runtimeType == typeof(Subscription);

        private static bool IsOperationExtensionType(Type? runtimeType)
        {
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

            return false;
        }
    }
}

using System;

using Geex.Gql.Types;

using HotChocolate.Types;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Descriptors.Definitions;

public static class ObjectTypeDefinitionExtensions
{
    extension(ObjectTypeDefinition definition)
    {
	    public bool IsOperationExtensionType() =>
		    IsOperationExtensionType(definition.RuntimeType, definition.Name);
    }

    private static bool IsOperationExtensionType(Type? runtimeType, string? typeName)
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

        return typeName is OperationTypeNames.Query
            or OperationTypeNames.Mutation
            or OperationTypeNames.Subscription;
    }
}

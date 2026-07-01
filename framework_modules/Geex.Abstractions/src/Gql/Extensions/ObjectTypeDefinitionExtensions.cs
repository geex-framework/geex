using System;

using Geex.Gql.AutoBatchLoad;
using Geex.Gql.Types;

using HotChocolate.Configuration;
using HotChocolate.Types;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Descriptors.Definitions;

public static class ObjectTypeDefinitionExtensions
{
    public static bool IsOperationObjectType(this ObjectTypeDefinition definition) =>
        IsOperationObjectType(definition.RuntimeType, definition.Name);

    public static bool IsAutoBatchLoadEnabled(
        this ObjectTypeDefinition definition,
        ITypeCompletionContext completionContext)
    {
        if (definition.ContextData.TryGetValue(AutoBatchLoadFeature.OperationContextDataKey, out var value) &&
            value is bool enabled)
        {
            return enabled;
        }

        return completionContext.IsAutoBatchLoadEnabled();
    }

    private static bool IsOperationObjectType(Type? runtimeType, string? typeName)
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
}

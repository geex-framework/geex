using System;

using Geex;
using Geex.Gql.Types;

using HotChocolate.Configuration;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Descriptors.Definitions;

public static class ObjectTypeDefinitionExtensions
{
    extension(ObjectTypeDefinition definition)
    {
	    public bool IsOperationExtensionType() =>
		    IsOperationExtensionType(definition.RuntimeType, definition.Name);

	    public bool IsAutoBatchLoadEnabled(ITypeCompletionContext completionContext)
	    {
		    if (definition.GeexFeatures.AutoBatchLoad.Enabled is bool enabled)
		    {
			    return enabled;
		    }

		    return completionContext.Services.GetRequiredService<GeexCoreModuleOptions>().AutoBatchLoad;
	    }
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

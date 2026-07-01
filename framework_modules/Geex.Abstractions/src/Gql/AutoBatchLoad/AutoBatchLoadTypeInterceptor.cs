using System;

using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace Geex.Gql.AutoBatchLoad
{
    public class AutoBatchLoadTypeInterceptor : TypeInterceptor
    {
        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            if (definition is ObjectTypeDefinition objectTypeDefinition)
            {
                TryInjectOperationFieldMiddleware(completionContext, objectTypeDefinition);
            }

            base.OnBeforeCompleteType(completionContext, definition);
        }

        private static void TryInjectOperationFieldMiddleware(
            ITypeCompletionContext completionContext,
            ObjectTypeDefinition objectTypeDefinition)
        {
            if (!objectTypeDefinition.IsOperationObjectType())
            {
                return;
            }

            if (!objectTypeDefinition.IsAutoBatchLoadEnabled(completionContext))
            {
                return;
            }

            foreach (var field in objectTypeDefinition.Fields)
            {
                if (ShouldSkipField(field))
                {
                    continue;
                }

                if (!field.IsEntityReturningField())
                {
                    continue;
                }

                field.ApplyAutoBatchLoadMiddleware();
            }
        }

        private static bool ShouldSkipField(ObjectFieldDefinition field) =>
            field.Name is "_" ||
            field.IsIntrospectionField ||
            field.Name.StartsWith("__", StringComparison.Ordinal);
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;

using Geex.Gql.Types;

using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

using MongoDB.Entities;

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
            if (!AutoBatchLoadGraphQL.IsOperationObjectType(objectTypeDefinition.RuntimeType, objectTypeDefinition.Name))
            {
                return;
            }

            if (!AutoBatchLoadFeature.IsAutoBatchLoadEnabled(completionContext, objectTypeDefinition))
            {
                return;
            }

            foreach (var field in objectTypeDefinition.Fields)
            {
                if (ShouldSkipField(field))
                {
                    continue;
                }

                if (!AutoBatchLoadGraphQL.IsQueryableEntityRootField(field) &&
                    !AutoBatchLoadGraphQL.IsObservableEntityRootField(field))
                {
                    continue;
                }

                AutoBatchLoadMiddlewareFactory.Apply(field);
            }
        }

        private static bool ShouldSkipField(ObjectFieldDefinition field) =>
            field.Name is "_" ||
            field.IsIntrospectionField ||
            field.Name.StartsWith("__", StringComparison.Ordinal);
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;

using Geex.Gql.Types;

using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Entities;

namespace Geex.Gql.AutoBatchLoad
{
    public class AutoBatchLoadTypeInterceptor : TypeInterceptor
    {
        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            if (definition is ObjectTypeDefinition objectTypeDefinition)
            {
                ValidateFieldLevelMisuse(objectTypeDefinition);
                TryInjectOperationFieldMiddleware(completionContext, objectTypeDefinition);
            }

            base.OnBeforeCompleteType(completionContext, definition);
        }

        private static void ValidateFieldLevelMisuse(ObjectTypeDefinition objectTypeDefinition)
        {
            if (OperationTypeHelper.IsOperationObjectType(objectTypeDefinition.RuntimeType, objectTypeDefinition.Name))
            {
                return;
            }

            foreach (var field in objectTypeDefinition.Fields)
            {
                if (field.ContextData.ContainsKey(AutoBatchLoadFeature.FieldContextDataKey))
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage(
                                $"UseAutoBatchLoad is only allowed on Query, Mutation, or Subscription operation types. " +
                                $"Field '{objectTypeDefinition.Name}.{field.Name}' is not allowed to configure auto batch load.")
                            .Build());
                }
            }
        }

        private static void TryInjectOperationFieldMiddleware(
            ITypeCompletionContext completionContext,
            ObjectTypeDefinition objectTypeDefinition)
        {
            if (!OperationTypeHelper.IsOperationObjectType(objectTypeDefinition.RuntimeType, objectTypeDefinition.Name))
            {
                return;
            }

            if (!AutoBatchLoadFeature.IsAutoBatchLoadEnabled(completionContext, objectTypeDefinition))
            {
                return;
            }

            foreach (var field in objectTypeDefinition.Fields)
            {
                if (ShouldSkipField(field) || !QueryableEntityFieldHelper.IsQueryableEntityRootField(field))
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

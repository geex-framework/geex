using System;

using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

using Microsoft.Extensions.DependencyInjection;

namespace Geex.Gql.AutoBatchLoad
{
    public class AutoBatchLoadTypeInterceptor : TypeInterceptor
    {
        private const string QueryTypeName = "Query";

        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            if (definition is not ObjectTypeDefinition objectTypeDefinition)
            {
                base.OnBeforeCompleteType(completionContext, definition);
                return;
            }

            if (!string.Equals(objectTypeDefinition.Name, QueryTypeName, StringComparison.Ordinal))
            {
                base.OnBeforeCompleteType(completionContext, definition);
                return;
            }

            var options = completionContext.Services.GetRequiredService<GeexCoreModuleOptions>();

            foreach (var field in objectTypeDefinition.Fields)
            {
                if (ShouldSkipField(field))
                {
                    continue;
                }

                var feature = field.GeexFeatures.AutoBatchLoadFeature;
                if (feature?.Enabled == false)
                {
                    continue;
                }

                if (feature is null && !options.AutoBatchLoadEnabled)
                {
                    continue;
                }

                if (feature is null)
                {
                    AutoBatchLoadMiddlewareFactory.Apply(field, enabled: true);
                }
            }

            base.OnBeforeCompleteType(completionContext, definition);
        }

        private static bool ShouldSkipField(ObjectFieldDefinition field)
        {
            return field.Name is "_" ||
                   field.IsIntrospectionField ||
                   field.Name.StartsWith("__", StringComparison.Ordinal);
        }
    }
}

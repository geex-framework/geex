using System;
using System.Linq;
using System.Threading.Tasks;

using Geex.Gql.GeexFeatures;

using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

using Microsoft.Extensions.DependencyInjection;

using static HotChocolate.WellKnownMiddleware;

namespace Geex.Gql.AutoBatchLoad
{
    internal static class AutoBatchLoadMiddlewareFactory
    {
        public static FieldMiddlewareDefinition CreateDefinition()
        {
            FieldMiddleware middleware = next => async context =>
            {
                var autoBatchLoad = context.Services.GetRequiredService<AutoBatchLoadMiddleware>();
                await autoBatchLoad.InvokeAsync(context, next).ConfigureAwait(false);
            };

            return new FieldMiddlewareDefinition(middleware, key: GeexFeatureKeys.AutoBatchLoadMiddleware);
        }

        public static void Apply(ObjectFieldDefinition definition, bool enabled)
        {
            definition.GeexFeatures.AutoBatchLoadFeature = new AutoBatchLoadFeature(enabled);

            if (!enabled)
            {
                return;
            }

            if (definition.MiddlewareDefinitions.Any(x => x.Key == GeexFeatureKeys.AutoBatchLoadMiddleware))
            {
                return;
            }

            var middlewareDefinition = CreateDefinition();
            var pagingIndex = IndexOfPagingMiddleware(definition);
            if (pagingIndex >= 0)
            {
                definition.MiddlewareDefinitions.Insert(pagingIndex + 1, middlewareDefinition);
                return;
            }

            definition.MiddlewareDefinitions.Add(middlewareDefinition);
        }

        private static int IndexOfPagingMiddleware(ObjectFieldDefinition definition)
        {
            for (var i = 0; i < definition.MiddlewareDefinitions.Count; i++)
            {
                if (definition.MiddlewareDefinitions[i].Key == Paging)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    public static class AutoBatchLoadFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor UseAutoBatchLoad(this IObjectFieldDescriptor descriptor, bool enabled)
        {
            descriptor.Extend().OnBeforeCreate((_, definition) =>
            {
                AutoBatchLoadMiddlewareFactory.Apply(definition, enabled);
            });

            return descriptor;
        }
    }
}

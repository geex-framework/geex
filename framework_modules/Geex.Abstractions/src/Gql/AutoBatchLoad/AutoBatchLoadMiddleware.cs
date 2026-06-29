using System;
using System.Linq;
using System.Threading.Tasks;

using Geex.Gql.Types;

using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Entities;
using MongoDB.Entities.Utilities;

namespace Geex.Gql.AutoBatchLoad
{
    public class AutoBatchLoadMiddleware
    {
        public async Task InvokeAsync(IMiddlewareContext context, FieldDelegate next)
        {
            if (!ShouldRun(context))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            if (!TryGetQueryableEntityElementType(context, out var entityType))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            var autoBatchLoadContext = context.Services.GetRequiredService<IAutoBatchLoadContext>();
            var selectionConfig = SelectionTreeWalker.Analyze(context, entityType);
            autoBatchLoadContext.PushOverlay(selectionConfig);
            try
            {
                await next(context).ConfigureAwait(false);
            }
            finally
            {
                autoBatchLoadContext.PopOverlay();
            }
        }

        private static bool ShouldRun(IMiddlewareContext context)
        {
            if (context.Selection.Field.IsIntrospectionField ||
                context.Selection.Field.Name is "_" ||
                context.Selection.Field.Name.StartsWith("__", StringComparison.Ordinal))
            {
                return false;
            }

            var operationType = context.ObjectType.RuntimeType;
            if (!IsOperationType(operationType))
            {
                return false;
            }

            if (context.ObjectType.ContextData.TryGetValue(
                    AutoBatchLoadFeature.OperationContextDataKey,
                    out var value) &&
                value is bool operationEnabled)
            {
                return operationEnabled;
            }

            var options = context.Services.GetService<GeexCoreModuleOptions>();
            return options?.AutoBatchLoad ?? true;
        }

        private static bool IsOperationType(Type runtimeType) =>
            runtimeType == typeof(Gql.Types.Query) ||
            runtimeType == typeof(Gql.Types.Mutation) ||
            runtimeType == typeof(Gql.Types.Subscription);

        private static bool TryGetQueryableEntityElementType(IMiddlewareContext context, out Type entityType)
        {
            entityType = null!;

            var resultType = context.Selection.Field.Type.ToRuntimeType();
            if (resultType == null)
            {
                return false;
            }

            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                resultType = resultType.GetGenericArguments()[0];
            }

            if (!resultType.IsGenericType || resultType.GetGenericTypeDefinition() != typeof(IQueryable<>))
            {
                return false;
            }

            entityType = resultType.GetGenericArguments()[0];
            return typeof(IEntityBase).IsAssignableFrom(entityType);
        }
    }

    internal static class AutoBatchLoadMiddlewareFactory
    {
        public static FieldMiddlewareDefinition CreateDefinition()
        {
            FieldMiddleware middleware = next => async context =>
            {
                var autoBatchLoad = context.Services.GetRequiredService<AutoBatchLoadMiddleware>();
                await autoBatchLoad.InvokeAsync(context, next).ConfigureAwait(false);
            };

            return new FieldMiddlewareDefinition(middleware, key: AutoBatchLoadFeature.MiddlewareKey);
        }

        public static void Apply(ObjectFieldDefinition definition)
        {
            if (definition.MiddlewareDefinitions.Any(x => x.Key == AutoBatchLoadFeature.MiddlewareKey))
            {
                return;
            }

            definition.MiddlewareDefinitions.Insert(0, CreateDefinition());
        }
    }
}

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

            if (!QueryableEntityFieldHelper.TryGetEntityElementType(context.Selection.Field, out var entityType))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            var selectionConfig = SelectionTreeWalker.Analyze(context, entityType);

            await next(context).ConfigureAwait(false);

            // Resolver 已返回原始 IQueryable（含手写 .BatchLoad()）；分页等外层 middleware 尚未包装结果
            TryApplySelectionBatchLoad(context, selectionConfig);
        }

        private static void TryApplySelectionBatchLoad(IMiddlewareContext context, BatchLoadConfig selectionConfig)
        {
            if (context.Result is not IQueryable queryable)
            {
                return;
            }

            if (queryable.Provider is not ICachedDbContextQueryProvider provider)
            {
                return;
            }

            provider.BatchLoadConfig.ApplySelectionBatchLoad(selectionConfig);
        }

        private static bool ShouldRun(IMiddlewareContext context)
        {
            if (context.Selection.Field.IsIntrospectionField ||
                context.Selection.Field.Name is "_" ||
                context.Selection.Field.Name.StartsWith("__", StringComparison.Ordinal))
            {
                return false;
            }

            if (!OperationTypeHelper.IsOperationObjectType(
                    context.ObjectType.RuntimeType,
                    context.ObjectType.Name))
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

            // 追加到链尾：紧贴 resolver，位于 UseOffsetPaging 等外层 middleware 之内侧，
            // 以便在分页包装前对 resolver 返回的 IQueryable 写入 BatchLoadConfig。
            definition.MiddlewareDefinitions.Add(CreateDefinition());
        }
    }
}

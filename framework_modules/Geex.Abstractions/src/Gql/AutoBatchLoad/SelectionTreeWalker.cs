using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MongoDB.Entities.Utilities;

namespace Geex.Gql.AutoBatchLoad
{
    internal static class SelectionTreeWalker
    {
        private static readonly HashSet<string> IgnoredFieldNames = new(StringComparer.Ordinal)
        {
            "__typename",
            "totalCount",
            "pageInfo",
            "_"
        };

        public static BatchLoadConfig Analyze(IMiddlewareContext context, Type entityType)
        {
            var config = new BatchLoadConfig();
            AppendEntitySelections(config, context, entityType, context.Selection);
            return config;
        }

        private static void AppendSelection(
            BatchLoadConfig config,
            IMiddlewareContext context,
            Type entityType,
            ISelection selection,
            IReadOnlyList<Type> excludedEntityTypes)
        {
            if (IgnoredFieldNames.Contains(selection.Field.Name))
            {
                return;
            }

            var property = selection.Field.ResolveNavigationProperty(entityType);
            if (property != null &&
                property.TryGetRelatedEntityType(out var relatedType) &&
                property.TryValidateBatchLoadable(entityType, out _))
            {
                var subConfig = config.RegisterBatchLoad(property, entityType, excludedEntityTypes);

                if (selection.SelectionSet == null)
                {
                    return;
                }

                AppendEntitySelections(subConfig, context, relatedType, selection);

                return;
            }

            AppendAutoBatchLoadDependsOn(config, entityType, selection, excludedEntityTypes);
        }

        private static void AppendAutoBatchLoadDependsOn(
            BatchLoadConfig config,
            Type entityType,
            ISelection selection,
            IReadOnlyList<Type> excludedEntityTypes)
        {
            var selectedProperty = selection.Field.ResolveEntityProperty(entityType);
            if (selectedProperty == null)
            {
                return;
            }

            foreach (var navigationPropertyName in selectedProperty.GetAutoBatchLoadDependsOnNavigationNames())
            {
                var navigationProperty = entityType.ResolveBatchLoadProperty(navigationPropertyName);
                if (navigationProperty == null ||
                    !navigationProperty.TryValidateBatchLoadable(entityType, out _))
                {
                    continue;
                }

                config.RegisterBatchLoad(navigationProperty, entityType, excludedEntityTypes);
            }
        }

        private static void AppendEntitySelections(
            BatchLoadConfig config,
            IMiddlewareContext context,
            Type declaredEntityType,
            ISelection selection)
        {
            if (selection.SelectionSet == null ||
                selection.Field is IObjectField field &&
                field.IsSystemOrIntrospectionField())
            {
                return;
            }

            if (selection.Field.IsRelayPagingField())
            {
                LogRelayPagingUnsupported(context, selection.Field);
                return;
            }

            if (selection.Field.IsOffsetPagingField() && selection.Field.Type.NamedType() is IObjectType pageType)
            {
                foreach (var items in context.GetSelections(pageType, selection, true)
                             .Where(item => item.Field.Name is "items" or "nodes"))
                {
                    AppendEntitySelections(config, context, declaredEntityType, items);
                }
                return;
            }

            var objectTypes = context.Operation.GetPossibleTypes(selection)
                .Where(type => declaredEntityType.IsAssignableFrom(type.RuntimeType))
                .ToArray();
            foreach (var objectType in objectTypes)
            {
                var entityType = objectType.RuntimeType;
                var excludedTypes = objectTypes.Select(type => type.RuntimeType)
                    .Where(type => type != entityType && entityType.IsAssignableFrom(type))
                    .Distinct().ToArray();
                foreach (var child in context.GetSelections(objectType, selection, true))
                {
                    AppendSelection(config, context, entityType, child, excludedTypes);
                }
            }
        }

        private static void LogRelayPagingUnsupported(IMiddlewareContext context, IOutputField field)
        {
            context.Services.GetService<ILogger<AutoBatchLoadMiddleware>>()?.LogWarning(
                "AutoBatchLoad 不支持 Relay/Cursor 分页字段 {FieldName}，嵌套导航无法自动 BatchLoad，可能发生 N+1 查询。\n {SyntaxNode}",
                field.Name,
                context.Selection.SyntaxNode.ToString());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

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
            var navigationEntityType = entityType.ResolveNavigationEntityType(context);
            var config = new BatchLoadConfig();
            var selections = GetEntityFieldSelections(context, navigationEntityType);

            foreach (var selection in selections)
            {
                AppendSelection(config, context, navigationEntityType, selection);
            }

            return config;
        }

        private static void AppendSelection(
            BatchLoadConfig config,
            IMiddlewareContext context,
            Type entityType,
            ISelection selection)
        {
            if (IgnoredFieldNames.Contains(selection.Field.Name))
            {
                return;
            }

            var property = selection.Field.ResolveNavigationProperty(entityType);
            if (property == null ||
                !property.TryGetRelatedEntityType(out var relatedType))
            {
                return;
            }

            if (!property.TryValidateBatchLoadable(entityType, out _))
            {
                return;
            }

            var subConfig = config.RegisterBatchLoad(property, entityType);

            if (selection.SelectionSet == null)
            {
                return;
            }

            var nestedEntityType = relatedType.ResolveNavigationEntityType(context);
            var nestedSelections = GetNestedEntityFieldSelections(context, nestedEntityType, selection);
            foreach (var nestedSelection in nestedSelections)
            {
                AppendSelection(subConfig, context, nestedEntityType, nestedSelection);
            }
        }

        private static IReadOnlyList<ISelection> GetEntityFieldSelections(
            IMiddlewareContext context,
            Type entityType)
        {
            if (context.Selection.Field is IObjectField field &&
                field.IsSystemOrIntrospectionField())
            {
                return Array.Empty<ISelection>();
            }

            if (context.Selection.Field.IsRelayPagingField())
            {
                LogRelayPagingUnsupported(context, context.Selection.Field);
                return Array.Empty<ISelection>();
            }

            if (context.Selection.Field.IsOffsetPagingField())
            {
                return GetEntitySelectionsUnderOffsetPaging(context, entityType, context.Selection, context.Selection.Field);
            }

            return GetNestedEntityFieldSelections(context, entityType, context.Selection);
        }

        private static IReadOnlyList<ISelection> GetNestedEntityFieldSelections(
            IMiddlewareContext context,
            Type entityType,
            ISelection selection)
        {
            if (selection.Field.IsRelayPagingField())
            {
                LogRelayPagingUnsupported(context, selection.Field);
                return Array.Empty<ISelection>();
            }

            if (selection.Field.IsOffsetPagingField())
            {
                return GetEntitySelectionsUnderOffsetPaging(context, entityType, selection, selection.Field);
            }

            if (entityType.TryResolveEntityObjectType(context, out var objectType))
            {
                return context.GetSelections(objectType, selection, true).ToArray();
            }

            return Array.Empty<ISelection>();
        }

        private static IReadOnlyList<ISelection> GetEntitySelectionsUnderOffsetPaging(
            IMiddlewareContext context,
            Type entityType,
            ISelection pagingSelection,
            IOutputField pagingField)
        {
            if (pagingField.Type.NamedType() is not IObjectType pageType)
            {
                return Array.Empty<ISelection>();
            }

            var pageSelections = context.GetSelections(pageType, pagingSelection, true).ToArray();
            var itemsSelection = pageSelections.FirstOrDefault(x => x.Field.Name is "items" or "nodes");
            if (itemsSelection == null)
            {
                return Array.Empty<ISelection>();
            }

            if (!entityType.TryResolveEntityObjectType(context, out var entityObjectType))
            {
                return Array.Empty<ISelection>();
            }

            return context.GetSelections(entityObjectType, itemsSelection, true).ToArray();
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

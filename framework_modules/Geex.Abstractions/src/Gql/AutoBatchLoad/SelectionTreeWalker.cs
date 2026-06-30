using System;
using System.Collections.Generic;
using System.Linq;

using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

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
            var navigationEntityType = EntityGraphQLTypeResolver.ResolveNavigationEntityType(context, entityType);
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

            var property = LazyNavigationMapper.ResolveNavigationProperty(entityType, selection.Field);
            if (property == null ||
                !LazyNavigationMapper.TryGetRelatedEntityType(property, out var relatedType))
            {
                return;
            }

            if (!BatchLoadNavigationValidator.TryValidate(property, entityType, out _))
            {
                return;
            }

            var subConfig = config.RegisterBatchLoad(property, entityType);

            if (selection.SelectionSet == null)
            {
                return;
            }

            var nestedEntityType = EntityGraphQLTypeResolver.ResolveNavigationEntityType(context, relatedType);
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
            if (context.Selection.Field.IsIntrospectionField)
            {
                return Array.Empty<ISelection>();
            }

            if (IsPagedField(context.Selection.Field))
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
            if (IsPagedField(selection.Field))
            {
                return GetEntitySelectionsUnderOffsetPaging(context, entityType, selection, selection.Field);
            }

            if (EntityGraphQLTypeResolver.TryResolveEntityObjectType(context, entityType, out var objectType))
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

            if (!EntityGraphQLTypeResolver.TryResolveEntityObjectType(context, entityType, out var entityObjectType))
            {
                return Array.Empty<ISelection>();
            }

            return context.GetSelections(entityObjectType, itemsSelection, true).ToArray();
        }

        private static bool IsPagedField(IOutputField field) =>
            field.Type.NamedType().Name.EndsWith("CollectionSegment", StringComparison.Ordinal);
    }
}

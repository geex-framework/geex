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
            var config = new BatchLoadConfig();
            var selections = GetEntityFieldSelections(context, entityType);

            foreach (var selection in selections)
            {
                AppendSelection(config, context, entityType, selection);
            }

            return config;
        }

        private static void AppendSelection(
            BatchLoadConfig config,
            IMiddlewareContext context,
            Type entityType,
            ISelection selection)
        {
            if (!ShouldIncludeSelection(context, selection))
            {
                return;
            }

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

            var subConfig = config.GetOrAddSubConfig(property);

            if (selection.SelectionSet == null)
            {
                return;
            }

            var nestedSelections = GetNestedEntityFieldSelections(context, relatedType, selection);
            foreach (var nestedSelection in nestedSelections)
            {
                AppendSelection(subConfig, context, relatedType, nestedSelection);
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

            if (TryResolveEntityObjectType(context, entityType, out var objectType))
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

            if (!TryResolveEntityObjectType(context, entityType, out var entityObjectType))
            {
                return Array.Empty<ISelection>();
            }

            return context.GetSelections(entityObjectType, itemsSelection, true).ToArray();
        }

        private static bool ShouldIncludeSelection(IMiddlewareContext context, ISelection selection) => true;

        private static bool IsPagedField(IOutputField field) =>
            field.Type.NamedType().Name.EndsWith("CollectionSegment", StringComparison.Ordinal);

        private static bool TryResolveEntityObjectType(
            IMiddlewareContext context,
            Type entityType,
            out IObjectType objectType)
        {
            foreach (var typeName in GetGraphQLTypeNameCandidates(entityType))
            {
                if (context.Schema.GetType<IObjectType>(typeName) is { } resolvedType)
                {
                    objectType = resolvedType;
                    return true;
                }
            }

            objectType = null!;
            return false;
        }

        private static IEnumerable<string> GetGraphQLTypeNameCandidates(Type entityType)
        {
            var typeName = entityType.Name;
            if (entityType.IsInterface &&
                typeName.StartsWith('I') &&
                typeName.Length > 1 &&
                char.IsUpper(typeName[1]))
            {
                yield return typeName[1..];
            }

            yield return typeName;
        }
    }
}

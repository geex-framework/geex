using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Geex.Gql.GeexFeatures;

using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MongoDB.Entities;
using MongoDB.Entities.Utilities;

namespace Geex.Gql.AutoBatchLoad
{
    public static class AutoBatchLoadExtensions
    {
        extension(IResolverContext context)
        {
            public BatchLoadConfig AnalyzeBatchLoadSelection(Type entityType) =>
                BatchLoadSelectionAnalysis.Analyze(context, entityType);

            public void MergeQueryableBatchLoad(IQueryable queryable, Type entityType)
            {
                var config = context.AnalyzeBatchLoadSelection(entityType);
                if (config.SubBatchLoadConfigCount == 0)
                {
                    return;
                }

                if (queryable?.Provider is not ICachedDbContextQueryProvider provider)
                {
                    context.Services
                        .GetService<ILogger<AutoBatchLoadMiddleware>>()
                        ?.LogWarning(
                            "Auto batch load config for {EntityType} was skipped because query provider {ProviderType} does not support batch load merging.",
                            entityType.Name,
                            queryable?.Provider?.GetType().FullName ?? "null");
                    return;
                }

                if (provider.BatchLoadConfig.SubBatchLoadConfigs.Count > 0)
                {
                    context.Services
                        .GetService<ILogger<AutoBatchLoadMiddleware>>()
                        ?.LogDebug(
                            "Auto batch load config for {EntityType} was skipped because the query already has manual BatchLoad configuration.",
                            entityType.Name);
                    return;
                }

                queryable.MergeBatchLoadConfig(config);
            }

            public void LoadEntityBatchLoad(IEntityBase entity) =>
                entity.LoadBatchLoad(context.AnalyzeBatchLoadSelection(entity.GetType()));
        }
    }

    internal static class BatchLoadSelectionAnalysis
    {
        private static readonly HashSet<string> IgnoredFieldNames = new(StringComparer.Ordinal)
        {
            "__typename",
            "totalCount",
            "pageInfo",
            "_"
        };

        public static BatchLoadConfig Analyze(IResolverContext context, Type entityType)
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
            IResolverContext context,
            Type entityType,
            ISelection selection)
        {
            if (IgnoredFieldNames.Contains(selection.Field.Name))
            {
                return;
            }

            var property = ResolveProperty(entityType, selection.Field);
            if (property == null ||
                !property.TryGetRelatedEntityType(out var relatedType) ||
                !BatchLoadHelper.IsRegisteredLazyQueryNavigation(entityType, property))
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

        private static IReadOnlyList<ISelection> GetEntityFieldSelections(IResolverContext context, Type entityType)
        {
            if (context.Selection.Field.HasOffsetPaging)
            {
                return GetEntitySelectionsUnderOffsetPaging(
                    context,
                    entityType,
                    context.Selection,
                    context.Selection.Field);
            }

            return GetNestedEntityFieldSelections(context, entityType, context.Selection);
        }

        private static IReadOnlyList<ISelection> GetNestedEntityFieldSelections(
            IResolverContext context,
            Type entityType,
            ISelection selection)
        {
            if (selection.Field.HasOffsetPaging)
            {
                return GetEntitySelectionsUnderOffsetPaging(context, entityType, selection, selection.Field);
            }

            if (TryResolveEntityObjectType(context, entityType, out var objectType))
            {
                return context.GetSelections(objectType, selection, true);
            }

            return Array.Empty<ISelection>();
        }

        private static IReadOnlyList<ISelection> GetEntitySelectionsUnderOffsetPaging(
            IResolverContext context,
            Type entityType,
            ISelection pagingSelection,
            IOutputField pagingField)
        {
            if (pagingField.Type.NamedType() is not IObjectType pageType)
            {
                return Array.Empty<ISelection>();
            }

            var pageSelections = context.GetSelections(pageType, pagingSelection, true);
            var itemsSelection = pageSelections.FirstOrDefault(x => x.Field.Name is "items" or "nodes");
            if (itemsSelection == null)
            {
                return Array.Empty<ISelection>();
            }

            if (!TryResolveEntityObjectType(context, entityType, out var entityObjectType))
            {
                return Array.Empty<ISelection>();
            }

            return context.GetSelections(entityObjectType, itemsSelection, true);
        }

        private static bool TryResolveEntityObjectType(
            IResolverContext context,
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

            context.Services
                .GetService<ILogger<AutoBatchLoadMiddleware>>()
                ?.LogWarning(
                    "Auto batch load selection analysis skipped nested fields because GraphQL object type for entity type '{EntityType}' could not be resolved.",
                    entityType.FullName);
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

        private static PropertyInfo? ResolveProperty(Type entityType, IOutputField field)
        {
            var fieldName = field.Name;
            var currentType = entityType;

            while (currentType != null)
            {
                var property = currentType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                    .FirstOrDefault(x =>
                        string.Equals(x.Name, fieldName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(char.ToLowerInvariant(x.Name[0]) + x.Name[1..], fieldName, StringComparison.Ordinal));

                if (property != null)
                {
                    return property;
                }

                currentType = currentType.BaseType;
            }

            if (field is IObjectField objectField && objectField.ResolverMember is PropertyInfo resolverProperty)
            {
                return resolverProperty;
            }

            if (field is IObjectField objectFieldWithMember && objectFieldWithMember.Member is PropertyInfo memberProperty)
            {
                return memberProperty;
            }

            return null;
        }
    }
}

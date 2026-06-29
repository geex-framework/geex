using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using MongoDB.Bson.Serialization;
using MongoDB.Entities;

namespace MongoDB.Entities.Utilities
{
    public static class BatchLoadExtensions
    {
        private static readonly Type ListType = typeof(List<>);
        private static readonly MethodInfo QueryableWhereMethodInfo = typeof(Queryable)
            .GetMethods()
            .First(m => m.Name == nameof(Queryable.Where) && m.GetParameters().Length == 2);

        public static void ApplySelectionOverlay(this BatchLoadConfig target, BatchLoadConfig selectionTree)
        {
            if (selectionTree == null)
            {
                return;
            }

            foreach (var (property, autoChildren) in selectionTree.SubBatchLoadConfigs)
            {
                if (!target.SubBatchLoadConfigs.TryGetValue(property, out var child))
                {
                    child = new BatchLoadConfig();
                    target.SubBatchLoadConfigs[property] = child;
                }

                ApplySelectionOverlay(child, autoChildren);
            }
        }

        public static void BatchLoadLazyQueries(this IQueryable entities, BatchLoadConfig batchLoadConfig)
        {
            if (batchLoadConfig?.SubBatchLoadConfigs.Count == 0)
            {
                return;
            }

            foreach (var (propertyInfo, subBatchLoadConfig) in batchLoadConfig.SubBatchLoadConfigs)
            {
                if (!TryGetSubQueryEntityType(propertyInfo, out var subQueryEntityType))
                {
                    continue;
                }

                var listType = ListType.MakeGenericTypeFast(subQueryEntityType);
                var lazyQueries = new List<ILazyQuery>();

                foreach (var entity in entities)
                {
                    if (entity is IEntityBase entityBase &&
                        entityBase.LazyQueryCache.TryGetValue(propertyInfo.Name, out var lazyQuery))
                    {
                        lazyQueries.Add(lazyQuery);
                    }
                }

                if (lazyQueries.Count == 0)
                {
                    continue;
                }

                var first = lazyQueries.First();
                var allQuery = first.DefaultSourceProvider().OfType(subQueryEntityType);
                var filterExpression = first.BatchQuery.DynamicInvoke(entities) as LambdaExpression;
                if (filterExpression == null)
                {
                    continue;
                }

                filterExpression = filterExpression.CastParamType(subQueryEntityType);

                var filteredQuery = (IQueryable)QueryableWhereMethodInfo
                    .MakeGenericMethodFast(subQueryEntityType)
                    .Invoke(null, [allQuery, filterExpression])!;

                var list = (IList)Activator.CreateInstance(listType, filteredQuery)!;
                var batchLoadResult = list.AsQueryable();

                if (list.Count > 0 && subBatchLoadConfig.SubBatchLoadConfigs.Count != 0)
                {
                    BatchLoadLazyQueries(batchLoadResult, subBatchLoadConfig);
                }

                foreach (var lazyQuery in lazyQueries)
                {
                    lazyQuery.Source = batchLoadResult;
                }
            }
        }

        public static bool ContainsSubConfig(this BatchLoadConfig config, PropertyInfo property) =>
            config.SubBatchLoadConfigs.ContainsKey(property);

        public static BatchLoadConfig GetOrAddSubConfig(this BatchLoadConfig config, PropertyInfo property)
        {
            if (!config.SubBatchLoadConfigs.TryGetValue(property, out var subConfig))
            {
                subConfig = new BatchLoadConfig();
                config.SubBatchLoadConfigs[property] = subConfig;
            }

            return subConfig;
        }

        private static bool TryGetSubQueryEntityType(PropertyInfo propertyInfo, out Type subQueryEntityType)
        {
            var propertyType = propertyInfo.PropertyType;
            Type relatedEntityType;

            if (propertyType.IsGenericType)
            {
                var genericDefinition = propertyType.GetGenericTypeDefinition();
                if (genericDefinition == typeof(IQueryable<>) || genericDefinition == typeof(Lazy<>))
                {
                    relatedEntityType = propertyType.GenericTypeArguments[0];
                }
                else
                {
                    subQueryEntityType = null!;
                    return false;
                }
            }
            else if (propertyType.Name is "Lazy`1" or "ResettableLazy`1" &&
                     propertyType.GenericTypeArguments.Length > 0)
            {
                relatedEntityType = propertyType.GenericTypeArguments[0];
            }
            else if (propertyType.IsAssignableTo<IQueryable>())
            {
                relatedEntityType = propertyType.GenericTypeArguments.First();
            }
            else
            {
                subQueryEntityType = null!;
                return false;
            }

            if (!typeof(IEntityBase).IsAssignableFrom(relatedEntityType))
            {
                subQueryEntityType = null!;
                return false;
            }

            subQueryEntityType = relatedEntityType.GetRootBsonClassMap().ClassType;
            return true;
        }
    }

    public static class BatchLoadMaterializationHooks
    {
        public static Func<BatchLoadConfig?>? ResolveSelectionOverlay { get; set; }

        internal static void ApplySelectionOverlayIfPresent(BatchLoadConfig target)
        {
            var overlay = ResolveSelectionOverlay?.Invoke();
            if (overlay != null && overlay.SubBatchLoadConfigs.Count > 0)
            {
                target.ApplySelectionOverlay(overlay);
            }
        }
    }
}

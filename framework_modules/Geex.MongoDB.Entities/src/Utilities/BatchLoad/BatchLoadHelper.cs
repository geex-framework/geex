using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using MongoDB.Bson.Serialization;
using MongoDB.Entities;
using MongoDB.Entities.Exceptions;

namespace MongoDB.Entities.Utilities
{
    public static class BatchLoadHelper
    {
        private static readonly Type ListType = typeof(List<>);
        private static readonly MethodInfo QueryableWhereMethodInfo = typeof(Queryable)
            .GetMethods()
            .First(m => m.Name == nameof(Queryable.Where) && m.GetParameters().Length == 2);
        private static readonly MethodInfo LoadEntitiesCoreMethod = typeof(BatchLoadHelper)
            .GetMethod(nameof(LoadEntitiesCore), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly ConcurrentDictionary<Type, Action<IReadOnlyList<IEntityBase>, BatchLoadConfig>> LoadEntitiesActions = new();
        private static readonly ConcurrentDictionary<Type, HashSet<string>> RegisteredLazyQueryPropertyNames = new();

        public static void MergeConfig(IQueryable queryable, BatchLoadConfig config)
        {
            if (config?.SubBatchLoadConfigs.Count == 0)
            {
                return;
            }

            if (queryable?.Provider is not ICachedDbContextQueryProvider provider)
            {
                return;
            }

            if (provider.BatchLoadConfig.SubBatchLoadConfigs.Count > 0)
            {
                return;
            }

            MergeConfigTree(provider.BatchLoadConfig, config);
        }

        public static void MergeConfigTree(BatchLoadConfig target, BatchLoadConfig source)
        {
            if (source == null)
            {
                return;
            }

            foreach (var (property, subConfig) in source.SubBatchLoadConfigs)
            {
                var existingSub = target.GetOrAddSubConfig(property);
                MergeConfigTree(existingSub, subConfig);
            }
        }

        public static BatchLoadConfig EnsurePath(BatchLoadConfig root, IReadOnlyList<PropertyInfo> path)
        {
            var current = root;
            foreach (var property in path)
            {
                current = current.GetOrAddSubConfig(property);
            }

            return current;
        }

        public static void LoadEntities(IEnumerable<IEntityBase> entities, BatchLoadConfig config)
        {
            var entityList = entities?.ToList();
            if (entityList == null || entityList.Count == 0 || config?.SubBatchLoadConfigs.Count == 0)
            {
                return;
            }

            var entityType = entityList[0].GetType().GetRootBsonClassMap().ClassType;
            LoadEntitiesActions.GetOrAdd(entityType, CreateLoadEntitiesAction)(entityList, config);
        }

        private static Action<IReadOnlyList<IEntityBase>, BatchLoadConfig> CreateLoadEntitiesAction(Type entityType)
        {
            var method = LoadEntitiesCoreMethod.MakeGenericMethod(entityType);
            return (entities, config) => method.Invoke(null, [entities, config]);
        }

        private static void LoadEntitiesCore<T>(IReadOnlyList<IEntityBase> entities, BatchLoadConfig config)
            where T : class, IEntityBase
        {
            BatchLoadLazyQueries(entities.Cast<T>().AsQueryable(), config);
        }

        public static void BatchLoadLazyQueries(IQueryable entities, BatchLoadConfig batchLoadConfig)
        {
            if (batchLoadConfig?.SubBatchLoadConfigs.Count == 0)
            {
                return;
            }

            foreach (var (propertyInfo, subBatchLoadConfig) in batchLoadConfig.SubBatchLoadConfigs)
            {
                if (!TryGetSubQueryEntityType(propertyInfo, out var subQueryEntityType))
                {
                    throw CreateConfigurationException(
                        propertyInfo.DeclaringType,
                        propertyInfo.Name,
                        $"Property '{propertyInfo.Name}' on '{propertyInfo.DeclaringType?.Name}' is included in batch load config but is not a lazy query navigation property.");
                }

                var listType = ListType.MakeGenericTypeFast(subQueryEntityType);
                var lazyQueries = new List<ILazyQuery>();
                var entityCount = 0;
                Type? entityType = null;

                foreach (var entity in entities)
                {
                    if (entity is not IEntityBase entityBase)
                    {
                        continue;
                    }

                    entityCount++;
                    entityType ??= entityBase.GetType().GetRootBsonClassMap().ClassType;

                    if (entityBase.LazyQueryCache.TryGetValue(propertyInfo.Name, out var lazyQuery))
                    {
                        lazyQueries.Add(lazyQuery);
                    }
                }

                if (entityCount == 0)
                {
                    continue;
                }

                if (lazyQueries.Count == 0)
                {
                    throw CreateConfigurationException(
                        entityType ?? propertyInfo.DeclaringType,
                        propertyInfo.Name,
                        $"Batch load requested for navigation property '{propertyInfo.Name}' on '{(entityType ?? propertyInfo.DeclaringType)?.Name}', but ConfigLazyQuery(...) is not configured.");
                }

                if (lazyQueries.Count != entityCount)
                {
                    throw CreateConfigurationException(
                        entityType ?? propertyInfo.DeclaringType,
                        propertyInfo.Name,
                        $"Batch load requested for navigation property '{propertyInfo.Name}' on '{(entityType ?? propertyInfo.DeclaringType)?.Name}', but only {lazyQueries.Count} of {entityCount} entities have ConfigLazyQuery(...) configured.");
                }

                var first = lazyQueries.First();
                var allQuery = first.DefaultSourceProvider().OfType(subQueryEntityType);
                var filterExpression = first.BatchQuery.DynamicInvoke(entities) as LambdaExpression;
                if (filterExpression == null)
                {
                    throw CreateConfigurationException(
                        entityType ?? propertyInfo.DeclaringType,
                        propertyInfo.Name,
                        $"Batch filter expression was null for navigation property '{propertyInfo.Name}' on '{(entityType ?? propertyInfo.DeclaringType)?.Name}'. Verify the ConfigLazyQuery batch rule.");
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

        public static bool IsLazyQueryNavigationProperty(PropertyInfo property) =>
            TryGetRelatedEntityType(property, out _);

        public static bool IsRegisteredLazyQueryNavigation(Type entityType, PropertyInfo property)
        {
            if (!TryGetRelatedEntityType(property, out _))
            {
                return false;
            }

            var registeredNames = RegisteredLazyQueryPropertyNames.GetOrAdd(
                entityType,
                static type => DiscoverRegisteredLazyQueryPropertyNames(type));
            return registeredNames.Contains(property.Name);
        }

        public static bool TryGetRelatedEntityType(PropertyInfo property, out Type relatedEntityType)
        {
            relatedEntityType = null!;
            var propertyType = property.PropertyType;

            if (propertyType.IsGenericType)
            {
                var genericDefinition = propertyType.GetGenericTypeDefinition();
                if (genericDefinition == typeof(IQueryable<>) || genericDefinition == typeof(Lazy<>))
                {
                    relatedEntityType = propertyType.GenericTypeArguments[0];
                    return typeof(IEntityBase).IsAssignableFrom(relatedEntityType);
                }
            }

            if (propertyType.Name is "Lazy`1" or "ResettableLazy`1" &&
                propertyType.GenericTypeArguments.Length > 0)
            {
                relatedEntityType = propertyType.GenericTypeArguments[0];
                return typeof(IEntityBase).IsAssignableFrom(relatedEntityType);
            }

            return false;
        }

        private static bool TryGetSubQueryEntityType(PropertyInfo propertyInfo, out Type subQueryEntityType)
        {
            if (!TryGetRelatedEntityType(propertyInfo, out var relatedEntityType))
            {
                subQueryEntityType = null!;
                return false;
            }

            subQueryEntityType = relatedEntityType.GetRootBsonClassMap().ClassType;
            return true;
        }

        private static HashSet<string> DiscoverRegisteredLazyQueryPropertyNames(Type entityType)
        {
            try
            {
                if (Activator.CreateInstance(entityType, nonPublic: true) is IEntityBase entity)
                {
                    return entity.LazyQueryCache.Keys.ToHashSet(StringComparer.Ordinal);
                }
            }
            catch
            {
                // ignored: analysis falls back to skipping unregistered navigations
            }

            return new HashSet<string>(StringComparer.Ordinal);
        }

        private static BatchLoadConfigurationException CreateConfigurationException(
            Type? entityType,
            string? propertyName,
            string message) =>
            new(entityType, propertyName, message);
    }
}

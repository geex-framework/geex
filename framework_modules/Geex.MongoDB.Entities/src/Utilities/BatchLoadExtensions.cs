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

        public static void BatchLoadLazyQueries(this IQueryable entities, BatchLoadConfig batchLoadConfig)
        {
            if (batchLoadConfig?.SubBatchLoadConfigs.Count == 0)
            {
                return;
            }

            foreach (var node in batchLoadConfig.SubBatchLoadConfigs.Values)
            {
                var propertyInfo = node.Property;
                var entityType = node.DeclaringEntityType;
                var subBatchLoadConfig = node.Children;

                if (!TryGetSubQueryEntityType(propertyInfo, out var subQueryEntityType))
                {
                    throw BatchLoadException.ExecutionFailed(
                        propertyInfo,
                        entityType,
                        $"属性类型 '{propertyInfo.PropertyType.Name}' 无法解析为实体 IQueryable/Lazy 导航");
                }

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

                var listType = ListType.MakeGenericTypeFast(subQueryEntityType);
                var first = lazyQueries.First();
                var allQuery = first.DefaultSourceProvider().OfType(subQueryEntityType);
                var filterExpression = first.BatchQuery.DynamicInvoke(entities) as LambdaExpression;
                if (filterExpression == null)
                {
                    throw BatchLoadException.ExecutionFailed(
                        propertyInfo,
                        entityType,
                        "BatchQuery 未返回有效的 LambdaExpression");
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

        public static BatchLoadConfig RegisterBatchLoad(
            this BatchLoadConfig config,
            PropertyInfo property,
            Type declaringEntityType)
        {
            BatchLoadNavigationValidator.Ensure(property, declaringEntityType);

            var canonicalProperty = BatchLoadNavigationValidator.ResolveCanonicalProperty(
                declaringEntityType,
                property.Name) ?? property;
            var key = new BatchLoadPathKey(declaringEntityType, property.Name);

            if (!config.SubBatchLoadConfigs.TryGetValue(key, out var node))
            {
                node = new BatchLoadPathNode(canonicalProperty, declaringEntityType);
                config.SubBatchLoadConfigs[key] = node;
            }

            return node.Children;
        }

        public static BatchLoadConfig GetSubConfig(
            this BatchLoadConfig config,
            PropertyInfo property,
            Type declaringEntityType)
        {
            var key = new BatchLoadPathKey(declaringEntityType, property.Name);
            return config.SubBatchLoadConfigs[key].Children;
        }

        /// <summary>
        /// 将 selection 分析结果注册到目标配置，语义等价于对每条路径调用
        /// <c>.BatchLoad()</c> / <c>.ThenBatchLoad()</c>；已存在的节点不会被覆盖。
        /// </summary>
        public static void ApplySelectionBatchLoad(this BatchLoadConfig target, BatchLoadConfig selectionTree)
        {
            if (selectionTree == null)
            {
                return;
            }

            foreach (var node in selectionTree.SubBatchLoadConfigs.Values)
            {
                var subConfig = target.RegisterBatchLoad(node.Property, node.DeclaringEntityType);
                subConfig.ApplySelectionBatchLoad(node.Children);
            }
        }

        private static bool TryGetSubQueryEntityType(PropertyInfo propertyInfo, out Type subQueryEntityType)
        {
            if (!BatchLoadNavigationValidator.TryGetRelatedEntityType(propertyInfo, out var relatedEntityType))
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
}

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
    public static class BatchLoadQueryExtensions
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
                    batchLoadResult.BatchLoadLazyQueries(subBatchLoadConfig);
                }

                foreach (var lazyQuery in lazyQueries)
                {
                    lazyQuery.Source = batchLoadResult;
                }
            }
        }

        private static bool TryGetSubQueryEntityType(PropertyInfo propertyInfo, out Type subQueryEntityType)
        {
            if (!propertyInfo.TryGetRelatedEntityType(out var relatedEntityType))
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

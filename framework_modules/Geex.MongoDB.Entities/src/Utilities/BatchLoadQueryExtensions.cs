using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Neleus.LambdaCompare;

namespace MongoDB.Entities.Utilities
{
    public static class BatchLoadQueryExtensions
    {
        private static readonly MethodInfo QueryableWhereMethodInfo = typeof(Queryable)
            .GetMethods()
            .First(method => method.Name == nameof(Queryable.Where) && method.GetParameters().Length == 2);

        public static void BatchLoadLazyQueries(this IQueryable entities, BatchLoadConfig batchLoadConfig)
        {
            if (batchLoadConfig == null || batchLoadConfig.SubBatchLoadConfigs.Count == 0) return;

            var groups = new Dictionary<(Type RuntimeType, string Navigation), List<LoadGroup>>();
            foreach (var entity in entities.Cast<IEntityBase>().Distinct<IEntityBase>(ReferenceEqualityComparer.Instance))
            {
                foreach (var node in batchLoadConfig.SubBatchLoadConfigs.Values)
                {
                    if (!node.AppliesTo(entity)) continue;
                    if (!entity.LazyQueryCache.TryGetValue(node.Property.Name, out var lazyQuery))
                    {
                        throw BatchLoadException.ExecutionFailed(node.Property, entity.GetType(),
                            "实体实例未配置已注册的 LazyQuery 导航");
                    }

                    var key = (entity.GetType(), node.Property.Name);
                    if (!groups.TryGetValue(key, out var candidates))
                    {
                        candidates = new List<LoadGroup>();
                        groups.Add(key, candidates);
                    }

                    var group = candidates.FirstOrDefault(candidate => candidate.CanInclude(entity, lazyQuery));
                    if (group == null)
                    {
                        group = new LoadGroup(entity, lazyQuery, node.Property);
                        candidates.Add(group);
                    }

                    group.Entities.Add(entity);
                    group.Queries.Add(lazyQuery);
                    group.Children.ApplySelectionBatchLoad(node.Children);
                }
            }

            foreach (var group in groups.Values.SelectMany(value => value)) Execute(group);
        }

        private static void Execute(LoadGroup group)
        {
            var first = group.FirstQuery;
            var metadata = first as IBatchLoadQueryMetadata;
            var sourceType = metadata?.SourceEntityType ?? ResolveSourceType(group);
            var relatedType = metadata?.RelatedEntityType;
            if (relatedType == null && !group.Property.TryGetRelatedEntityType(out relatedType))
            {
                throw BatchLoadException.ExecutionFailed(group.Property, group.FirstEntity.GetType(),
                    "属性类型无法解析为实体 IQueryable/Lazy 导航");
            }

            var sources = CreateTypedList(sourceType, group.Entities).AsQueryable();
            var filter = first.BatchQuery.DynamicInvoke(sources) as LambdaExpression;
            if (filter == null)
            {
                throw BatchLoadException.ExecutionFailed(group.Property, group.FirstEntity.GetType(),
                    "BatchQuery 未返回有效的 LambdaExpression");
            }

            // 保留导航的泛型类型, 持久化根类型转换由查询 Provider 处理.
            var allQuery = first.DefaultSourceProvider().OfType(relatedType);
            var filteredQuery = (IQueryable)QueryableWhereMethodInfo.MakeGenericMethod(relatedType)
                .Invoke(null, new object[] { allQuery, filter.CastParamType(relatedType) })!;
            var results = CreateTypedList(relatedType, filteredQuery).AsQueryable();
            if (group.Children.SubBatchLoadConfigs.Count != 0)
            {
                results.BatchLoadLazyQueries(group.Children);
            }

            foreach (var query in group.Queries) query.Source = results;
        }

        private static Type ResolveSourceType(LoadGroup group)
        {
            var parameters = group.FirstQuery.BatchQuery.GetType().GetMethod("Invoke")?.GetParameters();
            if (parameters is { Length: 1 } && parameters[0].ParameterType.IsGenericType &&
                parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(IQueryable<>))
            {
                return parameters[0].ParameterType.GetGenericArguments()[0];
            }

            throw BatchLoadException.ExecutionFailed(group.Property, group.FirstEntity.GetType(),
                "BatchQuery 必须接收一个 IQueryable<TEntity> 参数");
        }

        private static IList CreateTypedList(Type elementType, IEnumerable values)
        {
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
            foreach (var value in values) list.Add(value);
            return list;
        }

        private sealed class LoadGroup
        {
            public LoadGroup(IEntityBase entity, ILazyQuery query, PropertyInfo property)
            {
                FirstEntity = entity;
                FirstQuery = query;
                Property = property;
            }

            public IEntityBase FirstEntity { get; }
            public ILazyQuery FirstQuery { get; }
            public PropertyInfo Property { get; }
            public HashSet<IEntityBase> Entities { get; } = new(ReferenceEqualityComparer.Instance);
            public HashSet<ILazyQuery> Queries { get; } = new(ReferenceEqualityComparer.Instance);
            public BatchLoadConfig Children { get; } = new();

            public bool CanInclude(IEntityBase entity, ILazyQuery query)
            {
                if (ReferenceEquals(FirstQuery, query)) return true;
                if (FirstQuery is not IBatchLoadQueryMetadata first || query is not IBatchLoadQueryMetadata next ||
                    !first.UsesDefaultSource || !next.UsesDefaultSource ||
                    !ReferenceEquals(FirstEntity.DbContext, entity.DbContext) ||
                    first.SourceEntityType != next.SourceEntityType || first.RelatedEntityType != next.RelatedEntityType)
                {
                    return false;
                }

                // 捕获实例状态的规则和自定义来源不能由首个实体代表整组.
                return !CapturedValueDetector.ContainsCapture(first.BatchExpression) &&
                       !CapturedValueDetector.ContainsCapture(next.BatchExpression) &&
                       Lambda.ExpressionsEqual(first.BatchExpression, next.BatchExpression);
            }
        }

        private sealed class CapturedValueDetector : ExpressionVisitor
        {
            private bool _containsCapture;

            public static bool ContainsCapture(Expression expression)
            {
                var visitor = new CapturedValueDetector();
                visitor.Visit(expression);
                return visitor._containsCapture;
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                if (node.Value != null && node.Value is not string && !node.Type.IsValueType)
                {
                    _containsCapture = true;
                }
                return node;
            }
        }
    }
}

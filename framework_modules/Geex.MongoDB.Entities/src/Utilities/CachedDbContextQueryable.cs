using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using MongoDB.Bson.Serialization;
using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities.Utilities
{
    public class CachedDbContextQueryable<T, TSelect> : IOrderedQueryable<TSelect> where T : IEntityBase
    {
        private readonly DbContext _dbContext;
        private readonly Type _selectType = typeof(TSelect);
        private readonly Type _sourceType = typeof(T);
        private readonly Type _rootType;
        private static readonly Type ListType = typeof(List<>);

        public Type ElementType { get; } = typeof(T);
        public Expression Expression { get; }
        IQueryProvider IQueryable.Provider => TypedProvider;
        public IQueryProvider InnerProvider { get; }

        public CachedDbContextQueryProvider<T> TypedProvider { get; }

        public CachedDbContextQueryable(IQueryProvider provider, Expression expression)
        {
            this.TypedProvider = provider as CachedDbContextQueryProvider<T>;
            this.InnerProvider = TypedProvider.InnerProvider;
            var visitor = new StandardizeQueryExpressionVisitor();
            this.Expression = visitor.Visit(expression);
            this._dbContext = TypedProvider.DbContext;
            this._rootType = _sourceType.GetRootBsonClassMap().ClassType;
        }

        public IEnumerator<TSelect> GetEnumerator()
        {
            var expression = this.Expression;

            if (_dbContext == null)
            {
                return this.InnerProvider.CreateQuery<TSelect>(expression).GetEnumerator();
            }

            if (!this.TypedProvider.EntityTrackingEnabled)
            {
                var resultQuery = this.InnerProvider.CreateQuery<TSelect>(expression);

                if (_selectType.IsAssignableFrom(_sourceType) || _sourceType.IsAssignableFrom(_selectType))
                {
                    var entities = _selectType == _sourceType ? (IQueryable<T>)resultQuery : resultQuery.OfType<T>();
                    var attachedEntities = _dbContext.AttachNoTracking(entities).AsQueryable();
                    BatchLoadLazyQueries(attachedEntities, this.TypedProvider.BatchLoadConfig);
                    var finalResult = _selectType == _sourceType ? (IQueryable<TSelect>)attachedEntities : attachedEntities.OfType<TSelect>();
                    finalResult = PostFilter(finalResult);
                    return finalResult.GetEnumerator();
                }
                else if (this.TypedProvider.BatchLoadConfig.SubBatchLoadConfigs.Count != 0)
                {
                    var visitor = expression.ExtractQueryParts<T, TSelect>();
                    var dbQuery = this.InnerProvider.CreateQuery<T>(visitor.PreSelectExpression);
                    var dbEntities = dbQuery.ToList();
                    var attachedEntities = _dbContext.Attach(dbEntities).AsQueryable();

                    var resultQueryExpression = visitor.PreSelectExpression.ReplaceSource(attachedEntities, ReplaceType.OriginSource);
                    var entities = attachedEntities.Provider.CreateQuery<T>(resultQueryExpression);

                    BatchLoadLazyQueries(entities, this.TypedProvider.BatchLoadConfig);

                    if (visitor.PostSelectExpression != null)
                    {
                        var postSelectExpression = visitor.PostSelectExpression.ReplaceSource(entities, ReplaceType.SelectSource);
                        var finalResult = entities.Provider.CreateQuery<TSelect>(postSelectExpression);
                        finalResult = PostFilter(finalResult);
                        return finalResult.GetEnumerator();
                    }
                    else if (visitor.ExecuteExpression != null)
                    {
                        var result = entities.Provider.Execute<IEnumerable<TSelect>>(
                            visitor.ExecuteExpression.ReplaceSource(entities, ReplaceType.OriginSource));
                        result = PostFilter(result.AsQueryable());
                        return result.GetEnumerator();
                    }
                    else
                    {
                        var finalResult = _selectType == _sourceType ? (IQueryable<TSelect>)entities : entities.Cast<TSelect>().AsQueryable();
                        finalResult = PostFilter(finalResult);
                        return finalResult.GetEnumerator();
                    }
                }
                else
                {
                    var result = resultQuery.AsQueryable();
                    result = PostFilter(result);
                    return result.GetEnumerator();
                }
            }
            else
            {
                // 对于GroupBy查询，直接通过内部提供者执行
                var visitor = expression.ExtractQueryParts<T, TSelect>();
                if (visitor.IsGroupBySelectPattern)
                {
                    return this.InnerProvider.CreateQuery<TSelect>(expression).GetEnumerator();
                }
                var localEntities = _dbContext.MemoryDataCache[_rootType].Values.OfType<T>();
                var originLocalEntities = _dbContext.DbDataCache[_rootType].Values.OfType<T>();

                var localIds = localEntities.Select(x => x.Id).ToList();
                var deletedEntities = Enumerable.Empty<T>();

                if (localIds.Count != 0)
                {
                    deletedEntities = originLocalEntities.Where(x => !localIds.Contains(x.Id));
                }

                IQueryable<T> entities;

                if (localEntities.Any() || deletedEntities.Any())
                {
                    var dbQuery = this.InnerProvider.CreateQuery<T>(visitor.PreSelectExpression);

                    if (localIds.Count != 0)
                    {
                        dbQuery = dbQuery.Where(x => !localIds.Contains(x.Id));
                    }

                    var dbEntities = dbQuery.ToList();
                    _dbContext.UpdateDbDataCache(dbEntities);
                    var attachedDbEntities = _dbContext.Attach(dbEntities);

                    entities = localEntities.Union(attachedDbEntities).Except(deletedEntities).AsQueryable();
                    var resultQueryExpression = visitor.PreSelectExpression.ReplaceSource(entities, ReplaceType.OriginSource);
                    entities = entities.Provider.CreateQuery<T>(resultQueryExpression);
                }
                else
                {
                    var dbEntities = this.InnerProvider.CreateQuery<T>(visitor.PreSelectExpression).ToList();
                    entities = _dbContext.Attach(dbEntities).AsQueryable();
                }

                BatchLoadLazyQueries(entities, this.TypedProvider.BatchLoadConfig);

                if (visitor.PostSelectExpression != null)
                {
                    var postSelectExpression = visitor.PostSelectExpression.ReplaceSource(entities, ReplaceType.OriginSource);
                    var finalResult = entities.Provider.CreateQuery<TSelect>(postSelectExpression);
                    finalResult = PostFilter(finalResult);
                    return finalResult.GetEnumerator();
                }
                else if (visitor.ExecuteExpression != null)
                {
                    var result = entities.Provider.Execute<IEnumerable<TSelect>>(
                        visitor.ExecuteExpression.ReplaceSource(entities, ReplaceType.OriginSource));
                    result = PostFilter(result.AsQueryable());
                    return result.GetEnumerator();
                }
                else
                {
                    var finalResult = _selectType == _sourceType ? (IQueryable<TSelect>)entities : entities.Cast<TSelect>().AsQueryable();
                    finalResult = PostFilter(finalResult);
                    return finalResult.GetEnumerator();
                }
            }
        }

        private IQueryable<TSelect> PostFilter(IQueryable<TSelect> result)
        {
            if (_dbContext?.DataFilters.IsEmpty == false)
            {
                foreach (var (targetType, value) in _dbContext.DataFilters)
                {
                    if (targetType.IsAssignableFrom(_sourceType))
                    {
                        if (typeof(ExpressionDataFilter<>).MakeGenericType(targetType)
                                .GetProperty(nameof(ExpressionDataFilter<T>.PostFilterExpression))
                                ?.GetValue(value) is LambdaExpression originExpression)
                        {
                            var lambda = originExpression.CastParamType<TSelect>();
                            result = result.Where((Expression<Func<TSelect, bool>>)lambda);
                        }
                    }
                }
            }

            return result;
        }

        private void BatchLoadLazyQueries(IQueryable entities, BatchLoadConfig batchLoadConfig)
        {
            foreach (var (propertyInfo, subBatchLoadConfig) in batchLoadConfig.SubBatchLoadConfigs)
            {
                var subQueryEntityType = propertyInfo.PropertyType.GenericTypeArguments.First().GetRootBsonClassMap().ClassType;
                var listType = ListType.MakeGenericType(subQueryEntityType);

                var lazyQueries = new List<ILazyQuery>();

                foreach (var entity in entities)
                {
                    if (entity is IEntityBase entityBase && entityBase.LazyQueryCache.TryGetValue(propertyInfo.Name, out var lazyQuery))
                    {
                        lazyQueries.Add(lazyQuery);
                    }
                }

                if (lazyQueries.Count == 0) continue;

                var first = lazyQueries.First();
                var allQuery = first.DefaultSourceProvider().OfType(subQueryEntityType);

                var filterExpression = first.BatchQuery.DynamicInvoke(entities) as LambdaExpression;
                filterExpression = filterExpression.CastParamType(subQueryEntityType);

                var whereMethod = QueryableWhereMethodInfo.MakeGenericMethod(subQueryEntityType);
                var filteredQuery = (IQueryable)whereMethod.Invoke(null, [allQuery, filterExpression]);

                var list = (IList)Activator.CreateInstance(listType, filteredQuery);

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

        private static readonly MethodInfo QueryableWhereMethodInfo = typeof(Queryable)
            .GetMethods()
            .First(m => m.Name == nameof(Queryable.Where) && m.GetParameters().Length == 2);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}

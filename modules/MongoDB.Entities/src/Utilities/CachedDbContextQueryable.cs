using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using LinqToAnything;

using MethodTimer;

using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities.Utilities
{
    public class CachedDbContextQueryable<T, TSelect> : IOrderedQueryable<TSelect> where T : IEntityBase
    {
        private DbContext _dbContext;
        internal static MethodInfo enumerableWhereMethodInfo =
            typeof(Enumerable).GetMethods().First(x => x.Name == nameof(Enumerable.Where));
        internal static MethodInfo queryableWhereMethodInfo =
            typeof(Queryable).GetMethods().First(x => x.Name == nameof(Queryable.Where));
        internal static MethodInfo enumerableSelectMethodInfo =
            typeof(Enumerable).GetMethods().First(x => x.Name == nameof(Enumerable.Select));

        public Type ElementType { get; } = typeof(T);
        public Expression Expression { get; }
        IQueryProvider IQueryable.Provider => TypedProvider;
        public IQueryProvider InnerProvider { get; }

        public CachedDbContextQueryable(IQueryProvider provider, Expression expression)
        {
            this.TypedProvider = provider.As<CachedDbContextQueryProvider<T>>();
            this.InnerProvider = TypedProvider.InnerProvider;
            this.Expression = expression;
            this._dbContext = TypedProvider.DbContext;
        }

        public CachedDbContextQueryProvider<T> TypedProvider { get; set; }

        public IEnumerator<TSelect> GetEnumerator()
        {
            if (_dbContext == default)
            {
                return this.InnerProvider.CreateQuery<TSelect>(this.Expression).GetEnumerator();
            }
            if (!_dbContext.EntityTrackingEnabled)
            {
                IEnumerable<TSelect> result = this.InnerProvider.CreateQuery<TSelect>(this.Expression).AsEnumerable();
                if (typeof(TSelect).IsAssignableFrom(typeof(T)) || typeof(T).IsAssignableFrom(typeof(TSelect)))
                {
                    var sourceResult = _dbContext?.AttachNoTracking(result.Cast<T>());
                    BatchLoadLazyQueries(sourceResult.AsQueryable(), this.TypedProvider.BatchLoadConfig);
                    result = sourceResult.Cast<TSelect>();

                }
                return result.GetEnumerator();
            }
            else
            {
                var visitor = this.Expression.ExtractQueryParts<T, TSelect>();
                var rootType = typeof(T).GetRootBsonClassMap().ClassType;

                var localEntities = this._dbContext.Local[rootType].Values.OfType<T>();

                var originLocalEntities = this._dbContext.OriginLocal[rootType].Values.OfType<T>();

                var localIds = localEntities.Select(x => x.Id).ToList();
                var deletedEntities = Enumerable.Empty<T>();
                if (localIds.Any())
                {
                    deletedEntities = originLocalEntities.Where(x => !localIds.Contains(x.Id));
                }

                var dbQuery = this.InnerProvider.CreateQuery<T>(visitor.PreSelectExpression);
                if (localIds.Any())
                {
                    dbQuery = dbQuery.Where(x => !localIds.Contains(x.Id));
                }
                var dbEntities = dbQuery.ToList();
                if (dbEntities.Any())
                {
                    dbEntities = this._dbContext.Attach(dbEntities);
                }
                var entities = localEntities.Union(dbEntities).Except(deletedEntities).AsQueryable();
                var resultQueryExpression = visitor.PreSelectExpression.ReplaceSource(entities, ReplaceType.OriginSource);
                entities = entities.Provider.CreateQuery<T>(resultQueryExpression).AsQueryable();

                BatchLoadLazyQueries(entities, this.TypedProvider.BatchLoadConfig);

                IEnumerable<TSelect> result;
                if (visitor.PostSelectExpression != default)
                {
                    var postSelectExpression = visitor.PostSelectExpression.ReplaceSource(entities, ReplaceType.SelectSource);
                    //entities = entities.Provider.CreateQuery<T>(postSelectExpression);
                    var results = entities.Provider.CreateQuery<TSelect>(postSelectExpression);
                    return results.GetEnumerator();
                    //if (visitor.ExecuteExpression != default)
                    //{
                    //    return results.Provider.CreateQuery<TSelect>(visitor.ExecuteExpression.ReplaceSource(results, ReplaceType.DirectSource)).GetEnumerator();
                    //}
                }

                if (visitor.ExecuteExpression != default)
                {
                    result = entities.Provider.Execute<IEnumerable<TSelect>>(visitor.ExecuteExpression.ReplaceSource(entities, ReplaceType.OriginSource));
                }
                else
                {
                    result = entities.Cast<TSelect>();
                }

                if (_dbContext?.DataFilters.Any() == true)
                {
                    foreach (var (targetType, value) in _dbContext.DataFilters)
                    {
                        if (targetType.IsAssignableFrom(typeof(T)))
                        {
                            if (typeof(ExpressionDataFilter<>).MakeGenericType(targetType)
                                    .GetProperty(nameof(ExpressionDataFilter<T>.PostFilterExpression))
                                    ?.GetValue(value) is LambdaExpression originExpression)
                            {
                                var lambda = originExpression.CastParamType<T>();
                                result = Queryable.Where(result.AsQueryable(), (Expression<Func<TSelect, bool>>)lambda);
                            }
                        }
                    }
                }

                return result.GetEnumerator();
            }
        }

        static Type ListType = typeof(List<>);
        private void BatchLoadLazyQueries(IQueryable entities, BatchLoadConfig batchLoadConfig)
        {
            foreach (var (propertyInfo, subBatchLoadConfig) in batchLoadConfig.SubBatchLoadConfigs)
            {
                var subQueryType = propertyInfo.PropertyType;
                Type subQueryEntityType;
                if (subQueryType.IsAssignableTo<IQueryable>())
                {
                    subQueryEntityType = subQueryType.GenericTypeArguments.First().GetRootBsonClassMap().ClassType;
                }
                else
                {
                    subQueryEntityType = subQueryType.GetRootBsonClassMap().ClassType;
                }

                var listType = ListType.MakeGenericType(subQueryEntityType);
                IQueryable batchLoadResult = default;
                foreach (var entity in entities)
                {
                    if (!((IEntityBase)entity).LazyQueryCache.TryGetValue(propertyInfo.Name, out var lazyQuery))
                    {
                        throw new Exception("非LazyQuery不可进行BatchLoad, 请使用IQueryable.");
                    }

                    if (batchLoadResult == default)
                    {
                        var allQuery = lazyQuery.DefaultSourceProvider();
                        var filterExpression = lazyQuery.BatchQuery.As<LambdaExpression>().CompileFast()
                            .DynamicInvoke(entities).As<LambdaExpression>();
                        filterExpression = filterExpression.CastParamType(subQueryEntityType);
                        var filteredQuery = (IQueryable)queryableWhereMethodInfo.MakeGenericMethod(subQueryEntityType)
                            .Invoke(null, new object[] { allQuery.OfType(subQueryEntityType), filterExpression });

                        var list = (IList)Activator.CreateInstance(listType, filteredQuery);
                        batchLoadResult = list
                            .AsQueryable();

                        if (list.Count > 0)
                        {
                            if (subBatchLoadConfig.SubBatchLoadConfigs.Any())
                            {
                                this.BatchLoadLazyQueries(batchLoadResult, subBatchLoadConfig);
                            }
                        }
                    }

                    lazyQuery.Source = batchLoadResult;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using FastExpressionCompiler;

using Geex.MongoDB.Entities.Utilities;

using MongoDB.Bson.Serialization;
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
            var visitor = new StandardizeQueryExpressionVisitor();
            this.Expression = visitor.Visit(expression);
            this._dbContext = TypedProvider.DbContext;
        }

        public CachedDbContextQueryProvider<T> TypedProvider { get; set; }

        public IEnumerator<TSelect> GetEnumerator()
        {
            //var expression = ExpressionOptimizer.Visit(this.Expression);
            var expression = this.Expression;
            //var sw = Stopwatch.StartNew();
            var selectType = typeof(TSelect);
            var sourceType = typeof(T);
            try
            {
                //Debug.WriteLine($"CachedDbContextQueryable.GetEnumerator {selectType} started.");
                if (_dbContext == default)
                {
                    return this.InnerProvider.CreateQuery<TSelect>(expression).GetEnumerator();
                }

                if (!this.TypedProvider.EntityTrackingEnabled)
                {
                    IEnumerable<TSelect> result = ArraySegment<TSelect>.Empty;

                    // if select entity
                    if (selectType.IsAssignableFrom(sourceType) || sourceType.IsAssignableFrom(selectType))
                    {
                        result = this.InnerProvider.CreateQuery<TSelect>(expression).AsEnumerable();
                        var sourceResult = _dbContext?.AttachNoTracking(result.Cast<T>());
                        BatchLoadLazyQueries(sourceResult.AsQueryable(), this.TypedProvider.BatchLoadConfig);
                        result = sourceResult.Cast<TSelect>();
                    }
                    // if not select entity but with batch load config, force to load as entities
                    else if (this.TypedProvider.BatchLoadConfig.SubBatchLoadConfigs.Any())
                    {
                        var visitor = expression.ExtractQueryParts<T, TSelect>();
                        var dbQuery = this.InnerProvider.CreateQuery<T>(visitor.PreSelectExpression);
                        var dbEntities = dbQuery.ToList();
                        if (dbEntities.Any())
                        {
                            dbEntities = this._dbContext.Attach(dbEntities);
                        }
                        var entities = dbEntities.AsQueryable();

                        var resultQueryExpression = visitor.PreSelectExpression.ReplaceSource(entities, ReplaceType.OriginSource);
                        entities = entities.Provider.CreateQuery<T>(resultQueryExpression).AsQueryable();

                        BatchLoadLazyQueries(entities, this.TypedProvider.BatchLoadConfig);

                        if (visitor.PostSelectExpression != default)
                        {
                            var postSelectExpression =
                                visitor.PostSelectExpression.ReplaceSource(entities, ReplaceType.SelectSource);
                            var results = entities.Provider.CreateQuery<TSelect>(postSelectExpression);
                            return results.GetEnumerator();
                        }

                        if (visitor.ExecuteExpression != default)
                        {
                            result = entities.Provider.Execute<IEnumerable<TSelect>>(
                                visitor.ExecuteExpression.ReplaceSource(entities, ReplaceType.OriginSource));
                        }
                        else
                        {
                            result = entities.Cast<TSelect>();
                        }
                    }
                    // if not select entity and without batch load config
                    else
                    {
                        result = this.InnerProvider.CreateQuery<TSelect>(expression).AsEnumerable();
                    }

                    result = PostFilter(result);

                    var @return = result.GetEnumerator();
                    //Debug.WriteLine($"CachedDbContextQueryable.GetEnumerator {selectType} finished, within {sw.ElapsedMilliseconds}ms.");
                    return @return;
                }
                else
                {
                    var visitor = expression.ExtractQueryParts<T, TSelect>();
                    var rootType = sourceType.GetRootBsonClassMap().ClassType;

                    var localEntities = this._dbContext.Local[rootType].Values.OfType<T>();

                    var originLocalEntities = this._dbContext.OriginLocal[rootType].Values.OfType<T>();

                    var localIds = localEntities.Select(x => x.Id).ToList();
                    var deletedEntities = Enumerable.Empty<T>();
                    if (localIds.Any())
                    {
                        deletedEntities = originLocalEntities.Where(x => !localIds.Contains(x.Id));
                    }
                    IQueryable<T> entities;
                    if (!localEntities.Any() || !deletedEntities.Any())
                    {
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

                        entities = localEntities.Union(dbEntities).Except(deletedEntities).AsQueryable();
                        var resultQueryExpression =
                            visitor.PreSelectExpression.ReplaceSource(entities, ReplaceType.OriginSource);
                        entities = entities.Provider.CreateQuery<T>(resultQueryExpression).AsQueryable();
                    }
                    else
                    {
                        entities = this.InnerProvider.CreateQuery<T>(visitor.PreSelectExpression).ToList().AsQueryable();
                    }

                    BatchLoadLazyQueries(entities, this.TypedProvider.BatchLoadConfig);

                    IEnumerable<TSelect> result;
                    if (visitor.PostSelectExpression != default)
                    {
                        var postSelectExpression =
                            visitor.PostSelectExpression.ReplaceSource(entities, ReplaceType.SelectSource);
                        //entities = entities.Provider.CreateQuery<T>(postSelectExpression);
                        var results = entities.Provider.CreateQuery<TSelect>(postSelectExpression);
                        var @return = results.GetEnumerator();
                        //Debug.WriteLine($"CachedDbContextQueryable.GetEnumerator {selectType} finished, within {sw.ElapsedMilliseconds}ms.");
                        return @return;
                        //if (visitor.ExecuteExpression != default)
                        //{
                        //    return results.Provider.CreateQuery<TSelect>(visitor.ExecuteExpression.ReplaceSource(results, ReplaceType.DirectSource)).GetEnumerator();
                        //}
                    }

                    if (visitor.ExecuteExpression != default)
                    {
                        result = entities.Provider.Execute<IEnumerable<TSelect>>(
                            visitor.ExecuteExpression.ReplaceSource(entities, ReplaceType.OriginSource));
                    }
                    else
                    {
                        result = entities.Cast<TSelect>();
                    }

                    result = PostFilter(result);

                    var @return1 = result.GetEnumerator();
                    //Debug.WriteLine($"CachedDbContextQueryable.GetEnumerator {selectType} finished, within {sw.ElapsedMilliseconds}ms.");
                    return @return1;
                }
            }
            finally
            {

                //if (sw.ElapsedMilliseconds > 200)
                //{
                //Debug.WriteLine($"CachedDbContextQueryableProvider.Execute {sourceType}=>{selectType} takes long, query: {expression}");
                //}
            }

            IEnumerable<TSelect> PostFilter(IEnumerable<TSelect> result)
            {
                if (_dbContext?.DataFilters.Any() == true)
                {
                    foreach (var (targetType, value) in _dbContext.DataFilters)
                    {
                        if (targetType.IsAssignableFrom(sourceType))
                        {
                            if (typeof(ExpressionDataFilter<>).MakeGenericType(targetType)
                                    .GetProperty(nameof(ExpressionDataFilter<T>.PostFilterExpression))
                                    ?.GetValue(value) is LambdaExpression originExpression)
                            {
                                var lambda = originExpression.CastParamType<T>();
                                result = Queryable.Where(result.AsQueryable(),
                                    (Expression<Func<TSelect, bool>>)lambda);
                            }
                        }
                    }
                }

                return result;
            }
        }

        static Type ListType = typeof(List<>);
        private void BatchLoadLazyQueries(IQueryable entities, BatchLoadConfig batchLoadConfig)
        {
            foreach (var (propertyInfo, subBatchLoadConfig) in batchLoadConfig.SubBatchLoadConfigs)
            {
                var subQueryType = propertyInfo.PropertyType;
                var subQueryEntityType = subQueryType.GenericTypeArguments.First().GetRootBsonClassMap().ClassType;
                //if (subQueryType.IsAssignableTo<IQueryable>())
                //{
                //    subQueryEntityType = subQueryType.GenericTypeArguments.First().GetRootBsonClassMap().ClassType;
                //}
                //else
                //{
                //}
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

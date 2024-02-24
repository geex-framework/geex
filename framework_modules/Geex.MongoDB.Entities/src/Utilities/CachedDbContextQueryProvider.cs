using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using FastExpressionCompiler;
using MongoDB.Bson.Serialization;
using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities.Utilities
{
    public class BatchLoadConfig
    {
        internal Dictionary<PropertyInfo, BatchLoadConfig> SubBatchLoadConfigs { get; } = new Dictionary<PropertyInfo, BatchLoadConfig>();
    }
    public interface ICachedDbContextQueryProvider : IQueryProvider
    {
        public DbContext DbContext { get; set; }
        public bool EntityTrackingEnabled { get; set; }
        public BatchLoadConfig BatchLoadConfig { get; }
    }
    public class CachedDbContextQueryProvider<T> : ICachedDbContextQueryProvider where T : IEntityBase
    {
        public bool EntityTrackingEnabled { get; set; }
        public BatchLoadConfig BatchLoadConfig { get; } = new BatchLoadConfig();
        public DbContext DbContext { get; set; }
        public IQueryProvider InnerProvider { get; }
        internal static MethodInfo queryableSelectMethodInfo =
            typeof(Queryable).GetMethods().First(x => x.Name == nameof(Queryable.Select));

        internal static MethodInfo queryableWhereMethodInfo =
            typeof(Queryable).GetMethods().First(x => x.Name == nameof(Queryable.Where));

        internal static MethodInfo enumerableWhereMethodInfo =
            typeof(Enumerable).GetMethods().First(x => x.Name == nameof(Enumerable.Where));

        internal static Expression<Func<T, string>> idSelectExpression = (T x) => x.Id;

        internal static MethodInfo queryableFirstOrDefaultMethodInfo =
            typeof(Queryable).GetMethods().First(x => x.Name == nameof(Queryable.FirstOrDefault) && x.GetParameters().Length == 1);
        internal static MethodInfo queryableFirstMethodInfo =
            typeof(Queryable).GetMethods().First(x => x.Name == nameof(Queryable.First) && x.GetParameters().Length == 1);
        internal static MethodInfo queryableSingleOrDefaultMethodInfo =
            typeof(Queryable).GetMethods().First(x => x.Name == nameof(Queryable.SingleOrDefault) && x.GetParameters().Length == 1);
        internal static MethodInfo queryableSingleMethodInfo =
            typeof(Queryable).GetMethods().First(x => x.Name == nameof(Queryable.Single) && x.GetParameters().Length == 1);

        public bool PreFiltered { get; private set; }

        public CachedDbContextQueryProvider(IQueryProvider innerProvider, DbContext dbContext)
        {
            this.InnerProvider = innerProvider;
            this.EntityTrackingEnabled = dbContext?.EntityTrackingEnabled ?? false;
            this.DbContext = dbContext;
        }

        public IQueryable CreateQuery(Expression expression) => this.CreateQuery<T>(expression);
        /// <summary>
        /// 用于添加前置过滤器
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (!this.PreFiltered)
            {
                if (DbContext?.DataFilters.Any() == true)
                {
                    foreach (var (targetType, value) in DbContext.DataFilters)
                    {
                        if (targetType.IsAssignableFrom(typeof(T)))
                        {
                            if (typeof(ExpressionDataFilter<>).MakeGenericType(targetType).GetProperty(nameof(ExpressionDataFilter<T>.PreFilterExpression))?.GetValue(value) is LambdaExpression originExpression)
                            {
                                var lambda = originExpression.CastParamType<T>();
                                expression = Expression.Call(null, queryableWhereMethodInfo.MakeGenericMethod(typeof(T)), expression, lambda);
                            }
                        }
                    }
                }

                this.PreFiltered = true;
            }
            return new CachedDbContextQueryable<T, TElement>(this, expression);
        }

        object IQueryProvider.Execute(Expression expression) => (object)this.InnerProvider.Execute<T>(expression);

        public TResult Execute<TResult>(Expression expression)
        {
            if (this.DbContext != null)
            {
                if (!this.EntityTrackingEnabled)
                {
                    var result = this.InnerProvider.Execute<TResult>(expression);
                    if (typeof(TResult).IsAssignableFrom(typeof(T)) || typeof(T).IsAssignableFrom(typeof(TResult)))
                    {
                        result = (TResult)(object)DbContext.AttachNoTracking((T)(object)result);
                    }
                    return result;
                }
                if (expression is MethodCallExpression methodCallExpression)
                {
                    var visitor = expression.ExtractQueryParts<T, TResult>();

                    var localEntities = this.DbContext.Local[typeof(T)].Values.OfType<T>();

                    var originLocalEntities = this.DbContext.OriginLocal[typeof(T)].Values.OfType<T>();

                    var localIds = localEntities.Select(x => x.Id).ToList();
                    var deletedEntities = Enumerable.Empty<T>();
                    if (localIds.Any())
                    {
                        deletedEntities = originLocalEntities.Where(x => !localIds.Contains(x.Id));
                    }

                    var ts = this.CreateQuery<T>(visitor.PreSelectExpression);
                    var dbEntities = ts
                        //.Where(x => !localIds.Contains(x.Id))
                        .ToList();

                    if (dbEntities.Any())
                    {
                        dbEntities = this.DbContext.Attach(dbEntities);
                    }

                    var entities = localEntities.Union(dbEntities).Except(deletedEntities).AsQueryable();
                    var resultQueryExpression = visitor.PreSelectExpression.ReplaceSource(entities, ReplaceType.OriginSource);
                    entities = entities.Provider.CreateQuery<T>(resultQueryExpression).AsQueryable();

                    BatchLoadLazyQueries(entities, this.BatchLoadConfig);

                    TResult result;
                    if (visitor.PostSelectExpression != default)
                    {
                        var postSelectExpression = visitor.PostSelectExpression.ReplaceSource(entities, ReplaceType.SelectSource);
                        var results = entities.Provider.CreateQuery<TResult>(postSelectExpression);
                        if (visitor.ExecuteExpression != default)
                        {
                            return results.Provider.Execute<TResult>(visitor.ExecuteExpression.ReplaceSource(results, ReplaceType.DirectSource));
                        }
                    }

                    if (visitor.ExecuteExpression != default)
                    {
                        result = entities.Provider.Execute<TResult>(visitor.ExecuteExpression.ReplaceSource(entities, ReplaceType.OriginSource));

                    }
                    else
                    {
                        result = (TResult)entities;
                    }
                    return result;
                }

                throw new NotSupportedException();
            }

            var otherResult = this.InnerProvider.Execute<TResult>(expression);
            return otherResult;
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
                            .Invoke(null, new object[] { allQuery, filterExpression });

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
    }
}

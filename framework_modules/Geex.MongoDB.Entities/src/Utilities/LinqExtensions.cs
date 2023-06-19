using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Entities;
using MongoDB.Entities.Utilities;

// ReSharper disable once CheckNamespace
namespace System.Linq
{
    public static class LinqExtensions
    {
        static MethodInfo _queryableOfTypeMethod = typeof(Queryable).GetMethod(nameof(Queryable.OfType));
        static MethodInfo _enumerableOfTypeMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.OfType));

        /// <summary>
        /// 在集合查询被执行的时候进行后处理
        /// <br></br>注意, 后处理只应该在查询实际之前的末尾进行挂载, 任何的非继承树上的类型转换都会导致之前挂载的后处理失效
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="postAction"></param>
        /// <returns></returns>
        public static IQueryable<T> PostProcess<T>(this IQueryable<T> query, Func<T, T> postAction)
        {
            return new PostProcessQueryable<T>(query, postAction);
        }

        public static Task<T> OneAsync<T>(this IQueryable<T> query, string id, CancellationToken cancellationToken = default) where T : IEntityBase
        {
            return Task.FromResult(query.FirstOrDefault(x => x.Id == id));
        }

        public static IQueryable<T> FilterDefaults<T>(this IQueryable<T> query)
        {
            return query.Where(x => !x.Equals(default(T)));
        }
        public static List<T> FilterDefaults<T>(this List<T> query)
        {
            return query.Where(x => x != null && !x.Equals(default(T))).ToList();
        }
        public static List<TSelect> SelectList<T, TSelect>(this IQueryable<T> query, Expression<Func<T, TSelect>> selector)
        {
            return query.Select(selector).ToList();
        }

        public static IQueryable<TSource> OfType<TSource>(this IQueryable<TSource> queryable,
        Type runtimeType)
        {
            if (typeof(TSource) == runtimeType)
            {
                return queryable;
            }
            var generic = _queryableOfTypeMethod.MakeGenericMethod(new[] { runtimeType });
            return (IQueryable<TSource>)generic.Invoke(null, new[] { queryable });
        }

        public static IQueryable OfType(this IQueryable queryable,
        Type runtimeType)
        {
            var generic = _queryableOfTypeMethod.MakeGenericMethod(new[] { runtimeType });
            return (IQueryable)generic.Invoke(null, new[] { queryable });
        }

        //public static IEnumerable<TSource> OfType<TSource>(this IEnumerable<TSource> queryable,
        //Type runtimeType)
        //{
        //    if (typeof(TSource) == runtimeType)
        //    {
        //        return queryable;
        //    }
        //    var generic = _enumerableOfTypeMethod.MakeGenericMethod(new[] { runtimeType });
        //    return (IEnumerable<TSource>)generic.Invoke(null, new[] { queryable });
        //}
        public static IBatchLoadQueryable<TSource, TRelated> BatchLoad<TSource, TRelated>(this IQueryable<TSource> queryable,
            [NotNull] Expression<Func<TSource, IQueryable<TRelated>>> relatedQuery) where TRelated : IEntityBase where TSource : IEntityBase
        {
            if (relatedQuery == null) throw new ArgumentNullException(nameof(relatedQuery));
            var relatedProperty = relatedQuery.Body.As<MemberExpression>().Member.As<PropertyInfo>();
            return new BatchLoadQueryable<TSource, TRelated>(queryable, relatedProperty, true);
        }

        public static IBatchLoadQueryable<TSource, TRelated> BatchLoad<TSource, TRelated>(this IQueryable<TSource> queryable,
            [NotNull] Expression<Func<TSource, TRelated>> relatedQuery) where TRelated : IEntityBase where TSource : IEntityBase
        {
            if (relatedQuery == null) throw new ArgumentNullException(nameof(relatedQuery));
            var relatedProperty = relatedQuery.Body.As<MemberExpression>().Member.As<PropertyInfo>();
            return new BatchLoadQueryable<TSource, TRelated>(queryable, relatedProperty, true);
        }

        public static IBatchLoadQueryable<TSource, TThenRelated> ThenBatchLoad<TSource, TRelated, TThenRelated>(this IBatchLoadQueryable<TSource, TRelated> queryable, Expression<Func<TRelated, TThenRelated>> relatedQuery) where TRelated : IEntityBase where TSource : IEntityBase
        {
            if (relatedQuery == null) throw new ArgumentNullException(nameof(relatedQuery));
            var relatedProperty = relatedQuery.Body.As<MemberExpression>().Member.As<PropertyInfo>();
            var batchLoadQueryable = new BatchLoadQueryable<TSource, TThenRelated>(queryable, relatedProperty);
            return batchLoadQueryable;
        }

        public static IBatchLoadQueryable<TSource, TThenRelated> ThenBatchLoad<TSource, TRelated, TThenRelated>(this IBatchLoadQueryable<TSource, TRelated> queryable, Expression<Func<TRelated, IQueryable<TThenRelated>>> relatedQuery) where TRelated : IEntityBase where TSource : IEntityBase
        {
            if (relatedQuery == null) throw new ArgumentNullException(nameof(relatedQuery));
            var relatedProperty = relatedQuery.Body.As<MemberExpression>().Member.As<PropertyInfo>();
            var batchLoadQueryable = new BatchLoadQueryable<TSource, TThenRelated>(queryable, relatedProperty);
            return batchLoadQueryable;
        }

        internal static QueryPartsExpressionVisitor<TEntity, TResult> ExtractQueryParts<TEntity, TResult>(
            this Expression expression)
        {
            var visitor = new QueryPartsExpressionVisitor<TEntity, TResult>();
            visitor.Visit(expression);
            return visitor;
        }
    }
}

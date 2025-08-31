using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using MongoDB.Entities;
using MongoDB.Entities.Utilities;

// ReSharper disable once CheckNamespace
namespace System.Linq
{
    public static class LinqExtensions
    {
        static MethodInfo _queryableOfTypeMethod = typeof(Queryable).GetMethod(nameof(Queryable.OfType));
        static MethodInfo _queryableCastMethod = typeof(Queryable).GetMethod(nameof(Queryable.Cast));
        static MethodInfo _enumerableOfTypeMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.OfType));
        static MethodInfo _enumerableCastMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast));
        static MethodInfo _enumerableToListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList));
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

        /// <summary>
        /// 在集合查询被执行的时候进行后处理
        /// <br></br>注意, 后处理只应该在查询实际之前的末尾进行挂载, 任何的非继承树上的类型转换都会导致之前挂载的后处理失效
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="postAction"></param>
        /// <returns></returns>
        public static IQueryable<T> AsNoTracking<T>(this IQueryable<T> query) where T : IEntityBase
        {
            if (query is CachedDbContextQueryable<T, T> typedQueryable)
            {
                typedQueryable.TypedProvider.EntityTrackingEnabled = false;
            }
            return query;
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
            return (IQueryable<TSource>)_queryableOfTypeMethod.MakeGenericMethodFast(runtimeType).Invoke(
                null, [queryable]);
        }

        public static IQueryable OfType(this IQueryable queryable,
        Type runtimeType)
        {
            return (IQueryable)_queryableOfTypeMethod.MakeGenericMethodFast(runtimeType).Invoke(
                null, [queryable]);
        }

        public static IEnumerable<TSource> OfType<TSource>(this IEnumerable<TSource> enumerable,
        Type runtimeType)
        {
            if (typeof(TSource) == runtimeType)
            {
                return enumerable;
            }
            return (IEnumerable<TSource>)_enumerableOfTypeMethod.MakeGenericMethodFast(runtimeType).Invoke(
                null, [enumerable]);
        }

        public static IQueryable<TSource> Cast<TSource>(this IQueryable<TSource> queryable,
        Type runtimeType)
        {
            if (typeof(TSource) == runtimeType)
            {
                return queryable;
            }
            return (IQueryable<TSource>)_queryableCastMethod.MakeGenericMethodFast(runtimeType).Invoke(
                null, [queryable]);
        }

        public static IQueryable Cast(this IQueryable queryable,
        Type runtimeType)
        {
            return (IQueryable)_queryableCastMethod.MakeGenericMethodFast(runtimeType).Invoke(
                null, [queryable]);
        }

        public static IEnumerable Cast<TTarget>(this IEnumerable<TTarget> enumerable,
        Type runtimeType)
        {
            var casted = (IEnumerable)_enumerableCastMethod.MakeGenericMethodFast(runtimeType).Invoke(
                null, [enumerable]);
            var list = (IEnumerable)_enumerableToListMethod.MakeGenericMethodFast(runtimeType).Invoke(
                null, [casted]);
            return list;
        }

        public static IBatchLoadQueryable<TSource, TRelated> BatchLoad<TSource, TRelated>(this IQueryable<TSource> queryable,
            [NotNull] Expression<Func<TSource, IQueryable<TRelated>>> relatedQuery) where TRelated : IEntityBase where TSource : IEntityBase
        {
            if (relatedQuery == null) throw new ArgumentNullException(nameof(relatedQuery));
            var relatedProperty = relatedQuery.Body.As<MemberExpression>().Member.As<PropertyInfo>();
            return new BatchLoadQueryable<TSource, TRelated>(queryable, relatedProperty, true);
        }

        public static IBatchLoadQueryable<TSource, TRelated> BatchLoad<TSource, TRelated>(this IQueryable<TSource> queryable,
            [NotNull] Expression<Func<TSource, Lazy<TRelated>>> relatedQuery) where TRelated : IEntityBase where TSource : IEntityBase
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
            if (visitor is { HasGroupBy: true, IsGroupBySelectPattern: false })
            {
                throw new NotSupportedException("Group query cannot be used without projection(select).");
            }
            return visitor;
        }

        public static async Task ReplaceWhile<T>(
      this IList<T> source,
      Predicate<T> selector,
      Func<T, Task<T>> itemFactory)
        {
            for (int index = 0; index < source.Count; ++index)
            {
                T obj = source[index];
                if (selector(obj))
                    source[index] = await itemFactory(obj);
            }
        }
    }
}

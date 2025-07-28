using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using Geex;
using Geex.Abstractions;

using MongoDB.Entities;

using ZstdSharp;

// ReSharper disable once CheckNamespace
namespace System.Linq
{
    public static class LinqExtension
    {
        public static string FindCommonPrefix(this string str, params string[] more)
        {
            var prefixLength = str
                              .TakeWhile((c, i) => more.All(s => i < s.Length && s[i] == c))
                              .Count();

            return str[..prefixLength];
        }
        //public static List<TSource> ToList<TSource>(this IQueryable<TSource> source)
        //{
        //    if (source == null)
        //        throw new ArgumentNullException(nameof(source));
        //    return !(source is IIListProvider<TSource> ilistProvider) ? new List<TSource>(source) : ilistProvider.ToList();
        //}

        //public static IEnumerable<TResult> Cast<TEnum, TResult>(this IEnumerable<TEnum> source) where TEnum : Enumeration<TEnum, TResult> where TResult : IEquatable<TResult>, IComparable<TResult>
        //{
        //    if (source is IEnumerable<TResult> results)
        //        return results;
        //    if (source == null)
        //        throw new ArgumentNullException("source");
        //    return source.Select(x => (TResult)x);
        //}

        public static IEnumerable<TResult> CastEntity<TResult>(this IEnumerable source) where TResult : IEntityBase
        {
            if (source is IEnumerable<TResult> results)
                return results;

            ArgumentNullException.ThrowIfNull(source);

            // 检查源集合的元素类型是否实现了 IEntityBase
            var sourceType = source.GetType();
            if (sourceType.IsGenericType)
            {
                var elementType = sourceType.GetGenericArguments()[0];
                if (typeof(IEntityBase).IsAssignableFrom(elementType))
                {
                    return source.Cast<IEntityBase>().Select(x => x.CastEntity<TResult>());
                }
            }

            return source.Cast<TResult>();
        }

        public static IQueryable<TResult> CastEntity<TResult>(this IQueryable source) where TResult : IEntityBase
        {
            if (source is IQueryable<TResult> selfResults)
                return selfResults;

            ArgumentNullException.ThrowIfNull(source);

            // 检查源查询的元素类型是否实现了 IEntityBase
            if (typeof(IEntityBase).IsAssignableFrom(source.ElementType))
            {
                return source.Cast<IEntityBase>().Select(x => x.CastEntity<TResult>());
            }

            return source.Cast<TResult>();
        }

        //// 这个重载比标准Cast更具体，会被优先选择
        //public static IEnumerable<TResult> Cast<TSource, TResult>(this IEnumerable<TSource> source)
        //    where TSource : IEntityBase
        //    where TResult : IEntityBase
        //{
        //    ArgumentNullException.ThrowIfNull(source);
        //    return source.Select(x => x.Cast<TResult>());
        //}

        //public static IQueryable<TResult> Cast<TSource, TResult>(this IQueryable<TSource> source)
        //    where TSource : IEntityBase
        //    where TResult : IEntityBase
        //{
        //    ArgumentNullException.ThrowIfNull(source);

        //    var parameter = Expression.Parameter(typeof(TSource), "x");
        //    var castToIEntityBase = Expression.Convert(parameter, typeof(IEntityBase));
        //    var castMethod = typeof(IEntityBase).GetMethod("Cast").MakeGenericMethod(typeof(TResult));
        //    var castCall = Expression.Call(castToIEntityBase, castMethod);
        //    var lambda = Expression.Lambda<Func<TSource, TResult>>(castCall, parameter).FromSysExpression();

        //    return source.Select(lambda);
        //}


        public static IQueryable<TEntityType> WhereWithPostFilter<TEntityType>(this IQueryable<TEntityType> source, Expression<Func<TEntityType, bool>> expression)
        {
            var postExpression = default(Expression<Func<TEntityType, bool>>);
            Expression ProcessUncomputableExpressions(Expression exp, Expressions.ExpressionType mergeType)
            {
                switch (exp.NodeType)
                {
                    case Expressions.ExpressionType.AndAlso:
                        {
                            var binary = exp as BinaryExpression;
                            var left = binary.Left;
                            var right = binary.Right;
                            left = ProcessUncomputableExpressions(left, binary.NodeType);
                            right = ProcessUncomputableExpressions(right, binary.NodeType);
                            postExpression = Expression.Lambda<Func<TEntityType, bool>>(postExpression == default ? exp : Expression.AndAlso(postExpression.Body, exp), expression.Parameters);
                            exp = binary.Update(left, binary.Conversion, right);
                            break;
                        }
                    case Expressions.ExpressionType.OrElse:
                        {
                            var binary = exp as BinaryExpression;
                            var left = binary.Left;
                            var right = binary.Right;
                            left = ProcessUncomputableExpressions(left, binary.NodeType);
                            right = ProcessUncomputableExpressions(right, binary.NodeType);
                            postExpression = Expression.Lambda<Func<TEntityType, bool>>(postExpression == default ? exp : Expression.OrElse(postExpression.Body, exp), expression.Parameters);
                            exp = binary.Update(left, binary.Conversion, right);
                            break;
                        }
                    case Expressions.ExpressionType.Call:
                        {
                            var methodCallExp = exp as MethodCallExpression;
                            // bug:这里只处理了实例方法调用, 可能需要处理扩展方法
                            if (methodCallExp.Object is not MemberExpression memberExp) return exp;
                            while (memberExp.Expression is MemberExpression nestedMemberExp)
                            {
                                memberExp = nestedMemberExp;
                            }

                            if (memberExp.Member is not PropertyInfo propertyInfo) return exp;
                            //if (!PostFilterFields.ContainsKey(propertyInfo.GetHashCode())) return exp;
                            // 如果是postfilter属性

                            switch (mergeType)
                            {
                                case Expressions.ExpressionType.OrElse:
                                    postExpression = Expression.Lambda<Func<TEntityType, bool>>(postExpression == default ? exp : Expression.OrElse(postExpression.Body, exp), expression.Parameters);
                                    exp = Expression.Constant(true);
                                    return exp;
                                default:
                                    postExpression = Expression.Lambda<Func<TEntityType, bool>>(postExpression == default ? exp : Expression.AndAlso(postExpression.Body, exp), expression.Parameters);
                                    exp = Expression.Constant(true);
                                    return exp;
                            }
                        }
                    default:
                        {
                            var binary = exp as BinaryExpression;
                            if (binary.Left is not MemberExpression memberExp) return exp;
                            while (memberExp.Expression is MemberExpression nestedMemberExp)
                            {
                                memberExp = nestedMemberExp;
                            }

                            if (memberExp.Member is not PropertyInfo propertyInfo) return exp;
                            //if (!PostFilterFields.ContainsKey(propertyInfo.GetHashCode())) return exp;
                            // 如果是postfilter属性
                            switch (mergeType)
                            {
                                case Expressions.ExpressionType.OrElse:
                                    postExpression = Expression.Lambda<Func<TEntityType, bool>>(postExpression == default ? exp : Expression.OrElse(postExpression.Body, exp), expression.Parameters);
                                    exp = Expression.Constant(true);
                                    return exp;
                                default:
                                    postExpression = Expression.Lambda<Func<TEntityType, bool>>(postExpression == default ? exp : Expression.AndAlso(postExpression.Body, exp), expression.Parameters);
                                    exp = Expression.Constant(true);
                                    return exp;
                            }
                        }
                }
                return exp;
            }
            expression = expression.Update(ProcessUncomputableExpressions(expression.Body, expression.Body.NodeType), expression.Parameters);
            if (postExpression != default)
            {
                var data = source.Where<TEntityType>(expression).ToList().AsQueryable();
                data = data.Where<TEntityType>(postExpression);
                return data;
            }
            return source.Where<TEntityType>(expression);
        }

        public static T? GetById<T>(this IQueryable<T> query, string id) where T : IEntityBase
        {
            return query.FirstOrDefault(x => x.Id == id);
        }

        public static IQueryable<T> FilterByIds<T>(this IQueryable<T> query, params string[] ids) where T : class, IHasId
        {
            return query.Where(x => ids.Contains(x.Id));
        }
    }
}

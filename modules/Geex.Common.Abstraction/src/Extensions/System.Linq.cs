﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstractions;

using MongoDB.Entities;

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

        public static IQueryable<TEntityType> WhereWithPostFilter<TEntityType>(this IQueryable<TEntityType> source, Expression<Func<TEntityType, bool>> expression)
        {
            var postExpression = default(Expression<Func<TEntityType, bool>>);
            Expression ProcessUncomputableExpressions(Expression exp, ExpressionType mergeType)
            {
                switch (exp.NodeType)
                {
                    case ExpressionType.AndAlso:
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
                    case ExpressionType.OrElse:
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
                    case ExpressionType.Call:
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
                                case ExpressionType.OrElse:
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
                                case ExpressionType.OrElse:
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
            var data = source.Where<TEntityType>(expression).ToList().AsQueryable();
            if (postExpression != default)
            {
                data = data.Where<TEntityType>(postExpression);
            }

            return data;
        }

        public static T? GetById<T>(this IQueryable<T> query, string id) where T : EntityBase<T>
        {
            return query?.FirstOrDefault(x => x.Id == id);
        }

        public static IQueryable<T> FilterByIds<T>(this IQueryable<T> query, params string[] ids) where T : class, IHasId
        {
            return query.Where(x => ids.Contains(x.Id));
        }
    }
}

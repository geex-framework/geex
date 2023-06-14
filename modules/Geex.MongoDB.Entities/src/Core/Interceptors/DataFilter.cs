using System;
using System.Linq;
using System.Linq.Expressions;

using MongoDB.Driver;
using MongoDB.Entities.Utilities;

namespace MongoDB.Entities.Interceptors
{
    public interface IDataFilter
    {
        public LambdaExpression PreFilterExpression { get; }
        public LambdaExpression PostFilterExpression { get; }

        FilterDefinition<T> PreFilter<T>(FilterDefinition<T> filter);
        IQueryable<T> PostFilter<T>(IQueryable<T> filter);
    }
    /// <summary>
    /// invoke before entities are queried out
    /// </summary>
    public interface IDataFilter<TMarkerInterface> : IDataFilter
    {

    }


    public class ExpressionDataFilter<T> : IDataFilter<T>
    {
        public LambdaExpression PreFilterExpression { get; }
        public LambdaExpression PostFilterExpression { get; }

        /// <inheritdoc />
        public FilterDefinition<T1> PreFilter<T1>(FilterDefinition<T1> filter)
        {
            if (PreFilterExpression != null)
            {
                filter &= (Expression<Func<T1, bool>>)PreFilterExpression.CastParamType<T1>();
            }
            return filter;
        }

        /// <inheritdoc />
        public IQueryable<T1> PostFilter<T1>(IQueryable<T1> filter)
        {
            return PostFilterExpression != null ? filter.Where((Expression<Func<T1, bool>>)PostFilterExpression.CastParamType<T1>()) : filter;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="preFilterExpression">前置过滤条件, 只能包含简单的表达式, 不能创建新的闭包</param>
        /// <param name="postFilterExpression">后置过滤条件, 仅在数据库实际查询后内存过滤</param>
        public ExpressionDataFilter(LambdaExpression preFilterExpression, LambdaExpression postFilterExpression)
        {
            PreFilterExpression = preFilterExpression;
            PostFilterExpression = postFilterExpression;
        }
    }
}
using System;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate;
using MediatR;
using MongoDB.Entities;

namespace Geex.Common.Abstraction.Requests
{
    public class QueryRequest<T> : IRequest<IQueryable<T>> where T : IEntityBase
    {
        public QueryRequest()
        {

        }
        public static QueryRequest<T> New(Expression<Func<T, bool>> filter = default)
        {
            return new QueryRequest<T>(filter);
        }
        public QueryRequest(Expression<Func<T, bool>> filter = default)
        {
            Filter = filter;
        }
        public QueryRequest(params string[] ids)
        {
            Filter = x => ids.Contains(x.Id);
            Ids = ids;
        }
        [GraphQLIgnore]
        public Expression<Func<T, bool>> Filter { get; set; }
        /// <summary>
        /// 只在做id列表查询的时候有效
        /// </summary>
        [GraphQLIgnore]
        public string[] Ids { get; private set; }
        public string? _ { get; set; }
    }
}

using System;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate;
using MediatR;
using MongoDB.Entities;

// ReSharper disable once CheckNamespace
namespace Geex.Common.Requests
{
    public record QueryRequest<T> : IRequest<IQueryable<T>> where T : IEntityBase
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
        /// only works when query by id list
        /// </summary>
        [GraphQLIgnore]
        public string[] Ids { get; private set; }
        public string? _ { get; set; }
    }
}

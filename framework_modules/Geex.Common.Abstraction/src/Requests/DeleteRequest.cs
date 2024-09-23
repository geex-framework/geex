using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using HotChocolate;

using MediatR;

using MongoDB.Entities;

// ReSharper disable once CheckNamespace
namespace Geex.Common.Requests
{
    public record DeleteRequest<T> : IRequest<long> where T : IEntityBase
    {
        public DeleteRequest()
        {

        }
        public static DeleteRequest<T> New(Expression<Func<T, bool>> filter = default)
        {
            return new DeleteRequest<T>(filter);
        }
        public DeleteRequest(Expression<Func<T, bool>> filter = default)
        {
            Filter = filter;
        }
        public DeleteRequest(params string[] ids)
        {
            Filter = x => ids.Contains(x.Id);
            Ids = ids;
        }
        [GraphQLIgnore]
        public Expression<Func<T, bool>> Filter { get; set; }
        /// <summary>
        /// only works when deleting by id list
        /// </summary>
        [GraphQLIgnore]
        public string[] Ids { get; private set; }
        public string? _ { get; set; }
    }
}

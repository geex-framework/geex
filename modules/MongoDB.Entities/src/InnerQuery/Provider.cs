// The MIT License (MIT)
//
// Copyright (c) 2015 Pathmatics, Inc
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace MongoDB.Entities.InnerQuery
{
    /// <summary>
    /// Custom IQueryProvider that translates Expressions to MongoDB Aggregation framework queries
    /// </summary>
    /// <typeparam name="TDocument">The Mongo Document type to query against</typeparam>
    internal class MongoAggregationQueryProvider<TDocument> : IQueryProvider
    {
        private readonly IClientSessionHandle _session;
        private readonly AggregateOptions _options;

        /// <summary>The Mongo collection to query against</summary>
        private IMongoCollection<TDocument> _collection;

        public object Queryable { get; set; }
        public Action<string> LoggingDelegate { get; set; }

        /// <summary>
        /// Log a newline to the logging delegate
        /// </summary>
        private void LogLine(string s)
        {
            LoggingDelegate?.Invoke(s + Environment.NewLine);
        }

        public MongoAggregationQueryProvider(IMongoCollection<TDocument> collection, IClientSessionHandle session, AggregateOptions options)
        {
            _collection = collection;
            _session = session;
            _options = options;
        }

        /// <summary>No need to call this directly, required of IQueryProvider</summary>
        public IQueryable<TResult> CreateQuery<TResult>(Expression expression)
        {
            if (!typeof(IQueryable<TResult>).IsAssignableFrom(expression.Type))
                throw new ArgumentOutOfRangeException(nameof(expression));

            var queryable = new MongoAggregationQueryable<TResult>(_collection.CollectionNamespace.CollectionName)
            {
                Provider = this,
                Expression = expression
            };

            return queryable;
        }

        /// <summary>
        /// Executes the actual query.  Called automatically when the query is evaluated
        /// </summary>
        public TResult Execute<TResult>(Expression expression)
        {
            LogLine("----------------- EXPRESSION --------------------");
            var localExpression = expression;
            LogLine(expression.ToString());

            // Reduce any parts of the expression that can be evaluated locally
            var simplifiedExpression = ExpressionSimplifier.Simplify(this.Queryable, localExpression);
            if (simplifiedExpression != localExpression)
            {
                LogLine("----------------- SIMPLIFIED EXPRESSION --------------------");
                localExpression = simplifiedExpression;
                LogLine(localExpression.ToString());
            }

            var pipeline = new MongoPipeline<TDocument>(_collection, _session, _options, LoggingDelegate);
            try
            {
                return pipeline.Execute<TResult>(localExpression);
            }
            catch (MongoCommandException c)
            {
                if (c.Message.Contains("$in requires an array as a second argument, found: null"))
                    throw new ArgumentNullException(".Contains on null Enumerable blew up.", c);
                throw;
            }
        }

        /// <summary>I haven't seen this called yet...</summary>
        public IQueryable CreateQuery(Expression expression)
        {
            throw new NotImplementedException();
        }

        /// <summary>I haven't seen this called yet...</summary>
        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }
    }
}
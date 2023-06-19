﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 618

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a MongoDB Find command.
    /// <para>TIP: Specify your criteria using .Match() .Sort() .Skip() .Take() .Project() .Option() methods and finally call .Execute()</para>
    /// <para>Note: For building queries, use the DB.Fluent* interfaces</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public class Find<T> : Find<T, T> where T : IEntityBase
    {
        internal Find(DbContext dbContext) : base(dbContext) { }
    }

    /// <summary>
    /// Represents a MongoDB Find command with the ability to project to a different result type.
    /// <para>TIP: Specify your criteria using .Match() .Sort() .Skip() .Take() .Project() .Option() methods and finally call .Execute()</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    /// <typeparam name="TProjection">The type you'd like to project the results to.</typeparam>
    public class Find<T, TProjection> where T : IEntityBase
    {
        private FilterDefinition<T> filter = Builders<T>.Filter.Empty;
        private readonly Collection<SortDefinition<T>> sorts = new Collection<SortDefinition<T>>();
        private readonly FindOptions<T, TProjection> options = new FindOptions<T, TProjection>();

        internal Find(DbContext dbContext)
        {
            DbContext = dbContext;
        }

        /// <summary>
        /// Find a single IEntity by id
        /// </summary>
        /// <param name="id">The unique id of an IEntity</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <returns>A single entity or null if not found</returns>
        public Task<TProjection> OneAsync(string id, CancellationToken cancellation = default)
        {
            Match(id);
            return ExecuteSingleAsync(cancellation);
        }

        /// <summary>
        /// Find entities by supplying a lambda expression
        /// </summary>
        /// <param name="expression">x => x.Property == Value</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <returns>A list of Entities</returns>
        public Task<List<TProjection>> ManyAsync(Expression<Func<T, bool>> expression, CancellationToken cancellation = default)
        {
            Match(expression);
            return ExecuteAsync(cancellation);
        }

        /// <summary>
        /// Find entities by supplying a filter expression
        /// </summary>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <returns>A list of Entities</returns>
        public Task<List<TProjection>> ManyAsync(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter, CancellationToken cancellation = default)
        {
            Match(filter);
            return ExecuteAsync(cancellation);
        }

        /// <summary>
        /// Specify an IEntity id as the matching criteria
        /// </summary>
        /// <param name="id">A unique IEntity id</param>
        public Find<T, TProjection> MatchId(string id)
        {
            return Match(f => f.Eq(t => t.Id, id));
        }

        /// <summary>
        /// Specify an IEntity id as the matching criteria
        /// </summary>
        /// <param name="id">A unique IEntity id</param>
        public Find<T, TProjection> Match(string id)
        {
            return Match(f => f.Eq(t => t.Id, id));
        }

        /// <summary>
        /// Specify the matching criteria with a lambda expression
        /// </summary>
        /// <param name="expression">x => x.Property == Value</param>
        public Find<T, TProjection> Match(Expression<Func<T, bool>> expression)
        {
            return Match(f => f.Where(expression));
        }

        /// <summary>
        /// Specify the matching criteria with a filter expression
        /// </summary>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        public Find<T, TProjection> Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
        {
            this.filter &= filter(Builders<T>.Filter);
            return this;
        }

        /// <summary>
        /// Specify the matching criteria with a template
        /// </summary>
        /// <param name="template">A Template with a find query</param>
        public Find<T, TProjection> Match(Template template)
        {
            filter &= template.ToString();
            return this;
        }

        /// <summary>
        /// Specify a search term to find results from the text index of this particular collection.
        /// <para>TIP: Make sure to define a text index with DB.Index&lt;T&gt;() before searching</para>
        /// </summary>
        /// <param name="findSearchType">The type of text matching to do</param>
        /// <param name="searchTerm">The search term</param>
        /// <param name="caseSensitive">Case sensitivity of the search (optional)</param>
        /// <param name="diacriticSensitive">Diacritic sensitivity of the search (optional)</param>
        /// <param name="language">The language for the search (optional)</param>
        public Find<T, TProjection> Match(FindSearchType findSearchType, string searchTerm, bool caseSensitive = false, bool diacriticSensitive = false, string language = null)
        {
            if (findSearchType == FindSearchType.Fuzzy)
            {
                searchTerm = searchTerm.ToDoubleMetaphoneHash();
                caseSensitive = false;
                diacriticSensitive = false;
                language = null;
            }

            return Match(
                f => f.Text(
                    searchTerm,
                    new TextSearchOptions
                    {
                        CaseSensitive = caseSensitive,
                        DiacriticSensitive = diacriticSensitive,
                        Language = language
                    }));
        }

        /// <summary>
        /// Specify criteria for matching entities based on GeoSpatial data (longitude &amp; latitude)
        /// <para>TIP: Make sure to define a Geo2DSphere index with DB.Index&lt;T&gt;() before searching</para>
        /// <para>Note: DB.FluentGeoNear() supports more advanced options</para>
        /// </summary>
        /// <param name="coordinatesProperty">The property where 2DCoordinates are stored</param>
        /// <param name="nearCoordinates">The search point</param>
        /// <param name="maxDistance">Maximum distance in meters from the search point</param>
        /// <param name="minDistance">Minimum distance in meters from the search point</param>
        public Find<T, TProjection> Match(Expression<Func<T, object>> coordinatesProperty, Coordinates2D nearCoordinates, double? maxDistance = null, double? minDistance = null)
        {
            return Match(f => f.Near(coordinatesProperty, nearCoordinates, maxDistance, minDistance));
        }

        /// <summary>
        /// Specify the matching criteria with a JSON string
        /// </summary>
        /// <param name="jsonString">{ Title : 'The Power Of Now' }</param>
        public Find<T, TProjection> MatchString(string jsonString)
        {
            filter &= jsonString;
            return this;
        }

        /// <summary>
        /// Specify the matching criteria with an aggregation expression (i.e. $expr)
        /// </summary>
        /// <param name="expression">{ $gt: ['$Property1', '$Property2'] }</param>
        public Find<T, TProjection> MatchExpression(string expression)
        {
            filter &= "{$expr:" + expression + "}";
            return this;
        }

        /// <summary>
        /// Specify the matching criteria with a Template
        /// </summary>
        /// <param name="template">A Template object</param>
        public Find<T, TProjection> MatchExpression(Template template)
        {
            filter &= "{$expr:" + template.ToString() + "}";
            return this;
        }

        /// <summary>
        /// Specify which property and order to use for sorting (use multiple times if needed)
        /// </summary>
        /// <param name="propertyToSortBy">x => x.Prop</param>
        /// <param name="findSortType">The sort order</param>
        public Find<T, TProjection> Sort(Expression<Func<T, object>> propertyToSortBy, FindSortType findSortType)
        {
            switch (findSortType)
            {
                case FindSortType.Ascending:
                    return Sort(s => s.Ascending(propertyToSortBy));

                case FindSortType.Descending:
                    return Sort(s => s.Descending(propertyToSortBy));

                default:
                    return this;
            }
        }

        /// <summary>
        /// Sort the results of a text search by the MetaTextScore
        /// <para>TIP: Use this method after .Project() if you need to do a projection also</para>
        /// </summary>
        public Find<T, TProjection> SortByTextScore()
        {
            return SortByTextScore(null);
        }

        /// <summary>
        /// Sort the results of a text search by the MetaTextScore and get back the score as well
        /// <para>TIP: Use this method after .Project() if you need to do a projection also</para>
        /// </summary>
        /// <param name="scoreProperty">x => x.TextScoreProp</param>
        public Find<T, TProjection> SortByTextScore(Expression<Func<T, object>> scoreProperty)
        {
            switch (scoreProperty)
            {
                case null:
                    AddTxtScoreToProjection("_Text_Match_Score_");
                    return Sort(s => s.MetaTextScore("_Text_Match_Score_"));

                default:
                    AddTxtScoreToProjection(Prop.Path(scoreProperty));
                    return Sort(s => s.MetaTextScore(Prop.Path(scoreProperty)));
            }
        }

        /// <summary>
        /// Specify how to sort using a sort expression
        /// </summary>
        /// <param name="sortFunction">s => s.Ascending("Prop1").MetaTextScore("Prop2")</param>
        /// <returns></returns>
        public Find<T, TProjection> Sort(Func<SortDefinitionBuilder<T>, SortDefinition<T>> sortFunction)
        {
            sorts.Add(sortFunction(Builders<T>.Sort));
            return this;
        }

        /// <summary>
        /// Specify how many entities to skip
        /// </summary>
        /// <param name="skipCount">The number to skip</param>
        public Find<T, TProjection> Skip(int skipCount)
        {
            options.Skip = skipCount;
            return this;
        }

        /// <summary>
        /// Specify how many entities to Take/Limit
        /// </summary>
        /// <param name="takeCount">The number to limit/take</param>
        public Find<T, TProjection> Limit(int takeCount)
        {
            options.Limit = takeCount;
            return this;
        }

        /// <summary>
        /// Specify how to project the results using a lambda expression
        /// </summary>
        /// <param name="expression">x => new Test { PropName = x.Prop }</param>
        public Find<T, TProjection> Project(Expression<Func<T, TProjection>> expression)
        {
            return Project(p => p.Expression(expression));
        }

        /// <summary>
        /// Specify how to project the results using a projection expression
        /// </summary>
        /// <param name="projection">p => p.Include("Prop1").Exclude("Prop2")</param>
        public Find<T, TProjection> Project(Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, TProjection>> projection)
        {
            options.Projection = projection(Builders<T>.Projection);
            return this;
        }

        /// <summary>
        /// Specify how to project the results using a lambda expression
        /// </summary>
        /// <param name="expression">x => new Test { PropName = x.Prop }</param>
        public Find<T, TNewProjection> Project<TNewProjection>(Expression<Func<T, TNewProjection>> expression)
        {
            return Project<TNewProjection>(p => p.Expression(expression));
        }

        /// <summary>
        /// Specify how to project the results using a projection expression
        /// </summary>
        /// <param name="projection">p => p.Include("Prop1").Exclude("Prop2")</param>
        public Find<T, TNewProjection> Project<TNewProjection>(Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, TNewProjection>> projection)
        {
            var newProj = new Find<T, TNewProjection>(this.DbContext).Option(x =>
            {
                x.Projection = projection(Builders<T>.Projection);
                x.Limit = options.Limit;
                x.Skip = options.Skip;
                x.Sort = options.Sort;
                x.AllowDiskUse = options.AllowDiskUse;
                x.AllowPartialResults = options.AllowPartialResults;
                x.BatchSize = options.BatchSize;
                x.Collation = options.Collation;
                x.Comment = options.Comment;
                x.CursorType = options.CursorType;
                x.Hint = options.Hint;
                x.MaxAwaitTime = options.MaxAwaitTime;
                x.Max = options.Max;
                x.MaxTime = options.MaxTime;
                x.Min = options.Min;
                x.AllowPartialResults = options.AllowPartialResults;
                x.NoCursorTimeout = options.NoCursorTimeout;
                x.ReturnKey = options.ReturnKey;
                x.ShowRecordId = options.ShowRecordId;
            });
            return newProj;
        }

        /// <summary>
        /// Specify how to project the results using an exclusion projection expression.
        /// </summary>
        /// <param name="exclusion">x => new { x.PropToExclude, x.AnotherPropToExclude }</param>
        public Find<T, TProjection> ProjectExcluding(Expression<Func<T, object>> exclusion)
        {
            var props = (exclusion.Body as NewExpression)?.Arguments
                .Select(a => a.ToString().Split('.')[1]);

            if (!props.Any())
                throw new ArgumentException("Unable to get any properties from the exclusion expression!");

            var defs = new List<ProjectionDefinition<T>>();

            foreach (var prop in props)
            {
                defs.Add(Builders<T>.Projection.Exclude(prop));
            }

            options.Projection = Builders<T>.Projection.Combine(defs);

            return this;
        }

        /// <summary>
        /// Specify an option for this find command (use multiple times if needed)
        /// </summary>
        /// <param name="option">x => x.OptionName = OptionValue</param>
        public Find<T, TProjection> Option(Action<FindOptions<T, TProjection>> option)
        {
            option(options);
            return this;
        }

        /// <summary>
        /// Run the Find command in MongoDB server and get a list of results
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public async Task<List<TProjection>> ExecuteAsync(CancellationToken cancellation = default)
        {
            var projections = await
                (await ExecuteCursorAsync(cancellation).ConfigureAwait(false))
                .ToListAsync(cancellationToken: cancellation).ConfigureAwait(false);
            if (typeof(IEntityBase).IsAssignableFrom(typeof(TProjection)))
            {
                if (this.DbContext?.DataFilters?.IsEmpty == false)
                {
                    foreach (var (type, interceptor) in this.DbContext.DataFilters)
                    {
                        if (type.IsAssignableFrom(typeof(T)))
                        {
                            projections = interceptor.PostFilter(projections.AsQueryable()).ToList();
                        }
                    }
                }
                return projections.Select(projection => ((TProjection)this.DbContext?.AttachNoTracking(projection as IEntityBase) ?? projection)).ToList();
            }
            return projections;
        }

        public DbContext DbContext { get; }

        /// <summary>
        /// Run the Find command in MongoDB server and get a single result or the default value if not found.
        /// If more than one entity is found, it will throw an exception.
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public async Task<TProjection> ExecuteSingleAsync(CancellationToken cancellation = default)
        {
            var cursor = (await ExecuteCursorAsync(cancellation).ConfigureAwait(false));
            var projection = await
                cursor
                .SingleOrDefaultAsync(cancellationToken: cancellation).ConfigureAwait(false);
            if (this.DbContext?.DataFilters?.IsEmpty == false)
            {
                foreach (var (type, interceptor) in this.DbContext.DataFilters)
                {
                    if (type.IsAssignableFrom(typeof(T)))
                    {
                        projection = interceptor.PostFilter(new[] { projection }.AsQueryable()).Single();
                    }
                }
            }
            if (this.DbContext != default && projection is IEntityBase entity)
            {
                return (TProjection)this.DbContext.AttachNoTracking(entity);
            }
            return projection;
        }

        /// <summary>
        /// Run the Find command in MongoDB server and get the first result or the default value if not found
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public async Task<TProjection> ExecuteFirstAsync(CancellationToken cancellation = default)
        {
            var projection = await
                (await ExecuteCursorAsync(cancellation).ConfigureAwait(false))
                .FirstOrDefaultAsync(cancellationToken: cancellation).ConfigureAwait(false);
            if (this.DbContext?.DataFilters?.IsEmpty == false)
            {
                foreach (var (type, interceptor) in this.DbContext.DataFilters)
                {
                    if (type.IsAssignableFrom(typeof(T)))
                    {
                        projection = interceptor.PostFilter(new[] { projection }.AsQueryable()).FirstOrDefault();
                    }
                }
            }
            if (this.DbContext != default && projection is IEntityBase entity)
            {
                return (TProjection)this.DbContext.AttachNoTracking(entity);
            }
            return projection;
        }

        /// <summary>
        /// Run the Find command in MongoDB server and get a cursor instead of materialized results
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        [Obsolete("Not recommend, projected entities will lose its session.", false)]
        public Task<IAsyncCursor<TProjection>> ExecuteCursorAsync(CancellationToken cancellation = default)
        {
            if (sorts.Count > 0)
                options.Sort = Builders<T>.Sort.Combine(sorts);

            if (this.DbContext?.DataFilters?.IsEmpty == false)
            {
                foreach (var (type, interceptor) in this.DbContext.DataFilters)
                {
                    if (type.IsAssignableFrom(typeof(T)))
                    {
                        filter = interceptor.PreFilter(filter);
                    }
                }
            }
            // todo:post filter 放在外部调用方处理, 后期可以尝试优化
            return DbContext?.Session == null
                   ? DB.Collection<T>().FindAsync(filter, options, cancellation)
                   : DB.Collection<T>().FindAsync(DbContext.Session, filter, options, cancellation);
        }

        private void AddTxtScoreToProjection(string propName)
        {
            if (options.Projection == null) options.Projection = "{}";

            options.Projection =
                options.Projection
                .Render(BsonSerializer.SerializerRegistry.GetSerializer<T>(), BsonSerializer.SerializerRegistry)
                .Document.Add(propName, new BsonDocument { { "$meta", "textScore" } });
        }
    }

    public enum FindSortType
    {
        Ascending,
        Descending
    }

    public enum FindSearchType
    {
        Fuzzy,
        Full
    }
}

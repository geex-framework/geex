﻿using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents an update command
    /// <para>TIP: Specify a filter first with the .Match(). Then set property values with .Modify() and finally call .Execute() to run the command.</para>
    /// </summary>
    /// <typeparam name="T">Any class that implements IEntity</typeparam>
    public class Update<T> where T : IEntityBase
    {
        private readonly Collection<UpdateDefinition<T>> defs = new Collection<UpdateDefinition<T>>();
        private readonly Collection<PipelineStageDefinition<T, T>> stages = new Collection<PipelineStageDefinition<T, T>>();
        private FilterDefinition<T> filter = Builders<T>.Filter.Empty;
        private UpdateOptions options = new UpdateOptions();
        private readonly IClientSessionHandle session;
        private readonly Collection<UpdateManyModel<T>> models = new Collection<UpdateManyModel<T>>();

        internal Update(IClientSessionHandle session = null)
        {
            this.session = session;
        }

        /// <summary>
        /// Specify an IEntity id as the matching criteria
        /// </summary>
        /// <param name="id">A unique IEntity id</param>
        public Update<T> MatchId(string id)
        {
            return Match(f => f.Eq(t => t.Id, id));
        }

        /// <summary>
        /// Specify the IEntity matching criteria with a lambda expression
        /// </summary>
        /// <param name="expression">A lambda expression to select the Entities to update</param>
        public Update<T> Match(Expression<Func<T, bool>> expression)
        {
            return Match(f => f.Where(expression));
        }

        /// <summary>
        /// Specify the Entity matching criteria with a filter expression
        /// </summary>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        public Update<T> Match(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter)
        {
            this.filter &= filter(Builders<T>.Filter);
            return this;
        }

        /// <summary>
        /// Specify the Entity matching criteria with a Template
        /// </summary>
        /// <param name="template">The filter Template</param>
        public Update<T> Match(TemplateQuery template)
        {
            filter &= template.ToString();
            return this;
        }

        /// <summary>
        /// Specify the Entity matching criteria with a JSON string
        /// </summary>
        /// <param name="jsonString">{ Title : 'The Power Of Now' }</param>
        public Update<T> Match(string jsonString)
        {
            filter &= jsonString;
            return this;
        }

        /// <summary>
        /// Specify the property and it's value to modify (use multiple times if needed)
        /// </summary>
        /// <param name="property">x => x.Property</param>
        /// <param name="value">The value to set on the property</param>
        /// <returns></returns>
        public Update<T> Modify<TProp>(Expression<Func<T, TProp>> property, TProp value)
        {
            defs.Add(Builders<T>.Update.Set(property, value));
            return this;
        }

        /// <summary>
        /// Specify the update definition builder operation to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="operation">b => b.Inc(x => x.PropName, Value)</param>
        /// <returns></returns>
        public Update<T> Modify(Func<UpdateDefinitionBuilder<T>, UpdateDefinition<T>> operation)
        {
            defs.Add(operation(Builders<T>.Update));
            return this;
        }

        /// <summary>
        /// Specify an update (json string) to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="update">{ $set: { 'RootProp.$[x].SubProp' : 321 } }</param>
        public Update<T> Modify(string update)
        {
            defs.Add(update);
            return this;
        }

        /// <summary>
        /// Specify an update with a Template to modify the Entities (use multiple times if needed)
        /// </summary>
        /// <param name="template">A Template with a single update</param>
        public Update<T> Modify(TemplateQuery template)
        {
            Modify(template.ToString());
            return this;
        }

        /// <summary>
        /// Specify an update pipeline with multiple stages using a Template to modify the Entities.
        /// <para>NOTE: pipeline updates and regular updates cannot be used together.</para>
        /// </summary>
        /// <param name="template">A Template object containing multiple pipeline stages</param>
        public Update<T> WithPipeline(TemplateQuery template)
        {
            foreach (var stage in template.ToStages())
            {
                stages.Add(stage);
            }

            return this;
        }

        /// <summary>
        /// Specify an update pipeline stage to modify the Entities (use multiple times if needed)
        /// <para>NOTE: pipeline updates and regular updates cannot be used together.</para>
        /// </summary>
        /// <param name="stage">{ $set: { FullName: { $concat: ['$Name', ' ', '$Surname'] } } }</param>
        public Update<T> WithPipelineStage(string stage)
        {
            stages.Add(stage);
            return this;
        }

        /// <summary>
        /// Specify an update pipeline stage using a Template to modify the Entities (use multiple times if needed)
        /// <para>NOTE: pipeline updates and regular updates cannot be used together.</para>
        /// </summary>
        /// <param name="template">A Template object containing a pipeline stage</param>
        public Update<T> WithPipelineStage(TemplateQuery template)
        {
            return WithPipelineStage(template.ToString());
        }

        /// <summary>
        /// Specify an array filter to target nested entities for updates (use multiple times if needed).
        /// </summary>
        /// <param name="filter">{ 'x.SubProp': { $gte: 123 } }</param>
        public Update<T> WithArrayFilter(string filter)
        {
            ArrayFilterDefinition<T> def = filter;

            options.ArrayFilters =
                options.ArrayFilters == null
                ? new List<ArrayFilterDefinition>() { def }
                : options.ArrayFilters.Concat(new List<ArrayFilterDefinition> { def });

            return this;
        }

        /// <summary>
        /// Specify a single array filter using a Template to target nested entities for updates
        /// </summary>
        /// <param name="template"></param>
        public Update<T> WithArrayFilter(TemplateQuery template)
        {
            WithArrayFilter(template.ToString());
            return this;
        }

        /// <summary>
        /// Specify multiple array filters with a Template to target nested entities for updates.
        /// </summary>
        /// <param name="template">The template with an array [...] of filters</param>
        public Update<T> WithArrayFilters(TemplateQuery template)
        {
            var defs = template.ToArrayFilters<T>();

            options.ArrayFilters =
                options.ArrayFilters == null
                ? defs
                : options.ArrayFilters.Concat(defs);

            return this;
        }

        /// <summary>
        /// Specify an option for this update command (use multiple times if needed)
        /// <para>TIP: Setting options is not required</para>
        /// </summary>
        /// <param name="option">x => x.OptionName = OptionValue</param>
        public Update<T> Option(Action<UpdateOptions> option)
        {
            option(options);
            return this;
        }

        /// <summary>
        /// Queue up an update command for bulk execution later.
        /// </summary>
        public Update<T> AddToQueue()
        {
            if (filter == null) throw new ArgumentException("Please use Match() method first!");
            if (defs.Count == 0) throw new ArgumentException("Please use Modify() method first!");
            models.Add(new UpdateManyModel<T>(filter, Builders<T>.Update.Combine(defs)) { ArrayFilters = options.ArrayFilters });
            filter = Builders<T>.Filter.Empty;
            defs.Clear();
            options = new UpdateOptions();
            return this;
        }

        /// <summary>
        /// Run the update command in MongoDB.
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public async Task<UpdateResult> ExecuteAsync(CancellationToken cancellation = default)
        {
            if (models.Count > 0)
            {
                var bulkWriteResult = await (
                    session == null
                    ? DB.Collection<T>().BulkWriteAsync(models, null, cancellation)
                    : DB.Collection<T>().BulkWriteAsync(session, models, null, cancellation)
                    ).ConfigureAwait(false);

                models.Clear();

                return new UpdateResult.Acknowledged(bulkWriteResult.MatchedCount, bulkWriteResult.ModifiedCount, null);
            }
            else
            {
                if (filter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
                if (defs.Count == 0) throw new ArgumentException("Please use Modify() method first!");
                if (stages.Count > 0) throw new ArgumentException("Regular updates and Pipeline updates cannot be used together!");

                return await UpdateAsync(filter, Builders<T>.Update.Combine(defs), options, session, cancellation).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Run the update command with pipeline stages
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<UpdateResult> ExecutePipelineAsync(CancellationToken cancellation = default)
        {
            if (filter == Builders<T>.Filter.Empty) throw new ArgumentException("Please use Match() method first!");
            if (stages.Count == 0) throw new ArgumentException("Please use WithPipelineStage() method first!");
            if (defs.Count > 0) throw new ArgumentException("Pipeline updates cannot be used together with regular updates!");

            return UpdateAsync(
                filter,
                Builders<T>.Update.Pipeline(stages.ToArray()),
                options,
                session,
                cancellation);
        }

        private Task<UpdateResult> UpdateAsync(FilterDefinition<T> filter, UpdateDefinition<T> definition, UpdateOptions options, IClientSessionHandle session = null, CancellationToken cancellation = default)
        {
            return session == null
                   ? DB.Collection<T>().UpdateManyAsync(filter, definition, options, cancellation)
                   : DB.Collection<T>().UpdateManyAsync(session, filter, definition, options, cancellation);
        }
    }
}

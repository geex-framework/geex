using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using FastExpressionCompiler;

using Force.DeepCloner;

using KellermanSoftware.CompareNetObjects;

using Mapster;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Entities.Core.Comparers;
using MongoDB.Entities.Interceptors;
using MongoDB.Entities.Utilities;

using Volo.Abp;

using ReadConcern = MongoDB.Driver.ReadConcern;
using WriteConcern = MongoDB.Driver.WriteConcern;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a transaction used to carry out inter-related write operations.
    /// <para>TIP: Remember to always call .Dispose() after use or enclose in a 'Using' statement.</para>
    /// <para>IMPORTANT: Use the methods on this transaction to perform operations and not the methods on the DB class.</para>
    /// </summary>
    public class DbContext : IDisposable
    {

        static DbContext()
        {
            DbContext._compareLogic.Config.CustomComparers.Add(new JsonNodeComparer(RootComparerFactory.GetRootComparer()));
            TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();
            //TypeAdapterConfig.GlobalSettings.Default.Settings.ShouldMapMember.Add((model, side) =>
            //    model.SetterModifier.HasFlag(AccessModifier.Public | AccessModifier.Protected | AccessModifier.Internal) && !model.Type.IsValueType);
            //var assembly = Assembly.GetAssembly(typeof(DeepClonerExtensions));
            //var deepClonerSafeTypes = assembly.GetType("Force.DeepCloner.Helpers.DeepClonerSafeTypes");
            //var knownTypesField = deepClonerSafeTypes.GetField("KnownTypes", BindingFlags.Static | BindingFlags.NonPublic);
            //var knownTypes = (ConcurrentDictionary<Type, bool>)knownTypesField.GetValue(null);
            //knownTypes.TryAdd(typeof(DbContext), true);
            //knownTypes.TryAdd(typeof(IQueryable), true);
        }
        protected internal IClientSessionHandle
            session; //this will be set by Transaction class when inherited. otherwise null.

        public IClientSessionHandle Session
        {
            get => session;
        }
        public IMongoDatabase DefaultDb => DB.DefaultDb;

        /// <summary>
        /// 过滤器集合<br/>
        /// key = 被过滤的标记接口类型<br/>
        /// value = 过滤器工厂方法
        /// </summary>
        public static ConcurrentDictionary<Type, Func<IServiceProvider, IDataFilter>> StaticDataFilters { get; } = new()
        {
        };

        private ConcurrentDictionary<Type, IDataFilter> _dataFilters;
        /// <summary>
        /// static interceptors shortcut property.
        /// </summary>
        public ConcurrentDictionary<Type, IDataFilter> DataFilters
        {
            get
            {
                return _dataFilters ??= new(StaticDataFilters.ToDictionary(x => x.Key, x => x.Value(this.ServiceProvider)));
            }
        }

        /// <summary>
        /// Instantiates and begins a transaction.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="database">The name of the database to use for this transaction. default db is used if not specified</param>
        /// <param name="transactional"></param>
        /// <param name="options">Client session options for this transaction</param>
        public DbContext(IServiceProvider serviceProvider = default, string database = default,
            ClientSessionOptions? options = null, bool entityTrackingEnabled = true)
        {
            this.ServiceProvider = serviceProvider;
            EntityTrackingEnabled = entityTrackingEnabled;
            var mongoClient = DB.Database(database).Client;
            this.session = mongoClient.StartSession(options ?? DefaultSessionOptions);
            var topology = mongoClient.Cluster.Description.Type;
            this.SupportTransaction = topology is ClusterType.ReplicaSet or ClusterType.Sharded;
        }

        public bool SupportTransaction { get; set; }

        public IServiceProvider ServiceProvider { get; }
        public bool EntityTrackingEnabled { get; internal set; }

        /// <summary>
        /// Gets an accurate count of how many entities are matched for a given expression/filter in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="expression">A lambda expression for getting the count for a subset of the data</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> CountAsync<T>(Expression<Func<T, bool>> expression,
            CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.CountAsync(expression, this, cancellation);
        }

        public virtual void RemoveDataFilters(params Type[] filteredInterfaces)
        {
            foreach (var filterInterface in filteredInterfaces)
            {
                DataFilters.Remove(filterInterface, out _);
            }
        }

        public virtual IDisposable DisableDataFilters(params Type[] filteredInterfaces)
        {
            var disabledFilters = new ConcurrentDictionary<Type, IDataFilter>();
            foreach (var filterInterface in filteredInterfaces)
            {
                DataFilters.Remove(filterInterface, out var disabledDataFilter);
                disabledFilters.TryAdd(filterInterface, disabledDataFilter);
            }
            return new FilterDisabledContext(this, disabledFilters);
        }

        public virtual IDisposable DisableAllDataFilters()
        {
            var context = new FilterDisabledContext(this, new ConcurrentDictionary<Type, IDataFilter>(DataFilters));
            DataFilters.Clear();
            return context;
        }

        public virtual void RegisterDataFilters(params IDataFilter[] dataFilters)
        {
            foreach (var dataFilter in dataFilters)
            {
                var filterType = dataFilter.GetType();
                // bug:这里只找了两层, 需要优化
                DataFilters.AddOrUpdate(filterType.GenericTypeArguments.ElementAtOrDefault(0) ?? filterType.BaseType.GenericTypeArguments.ElementAtOrDefault(0), dataFilter, (_, _) => dataFilter);
            }
        }

        public static void RemoveDataFiltersForAll(params Type[] filteredInterfaces)
        {
            foreach (var filterInterface in filteredInterfaces)
            {
                StaticDataFilters.Remove(filterInterface, out _);
            }
        }

        public virtual T AttachNoTracking<T>(T entity) where T : IEntityBase
        {
            if (entity == null)
            {
                return default(T);
            }
            var isNew = entity.Id == default;
            if (isNew)
            {
                entity.Id = entity.GenerateNewId().ToString();
                entity.CreatedOn = DateTimeOffset.Now;
            }
            entity.DbContext = this;
            return entity;
        }
        public virtual IEnumerable<T> AttachNoTracking<T>(IEnumerable<T> entities) where T : IEntityBase
        {
            return entities.Select(this.AttachNoTracking).ToList();
        }

        public virtual T Attach<T>(T entity) where T : IEntityBase
        {
            if (entity == null)
            {
                return default(T);
            }

            if (!this.EntityTrackingEnabled)
            {
                return this.AttachNoTracking(entity);
            }

            var isNew = entity.Id == default;
            var now = DateTimeOffset.Now;
            if (isNew)
            {
                entity.Id = entity.GenerateNewId().ToString();
                entity.CreatedOn = now;
            }

            if (entity is IModifiedOn modifiedOn)
                modifiedOn.ModifiedOn = now;
            var rootType = entity.GetType().GetRootBsonClassMap().ClassType;
            if (this.MemoryDataCache[rootType].TryGetValue(entity.Id, out var existed))
            {
                return existed.CastEntity<T>();
            }
            else
            {
                if (this.MemoryDataCache[rootType].Count > 10000)
                {
                    //throw new NotSupportedException("Queryable Api不支持数量过多的Entity查询/写入, 请考虑使用原生Api");
                }
                this.MemoryDataCache[rootType].TryAdd(entity.Id, entity);
                entity.DbContext = this;
                if (entity is IAttachIntercepted intercepted)
                {
                    intercepted.InterceptOnAttached();
                }
                return entity;
            }
        }
        /// <summary>
        /// 本地内存缓存
        /// </summary>
        public DbContextCache MemoryDataCache { get; set; } = new();
        /// <summary>
        /// 本地内存缓存(数据库值)
        /// </summary>
        public DbContextCache DbDataCache { get; set; } = new();

        public virtual IEnumerable<T> Attach<T>(IEnumerable<T> entities) where T : IEntityBase
        {
            return entities.Select(this.Attach).ToList();
        }
        public virtual List<T> Attach<T>(List<T> entities) where T : IEntityBase
        {
            entities.ReplaceWhile(x => true, x => this.Attach(x));
            return entities;
        }
        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> CountAsync<T>(CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.CountAsync<T>(_ => true, this, cancellation);
        }

        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="filter">A filter definition</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> CountAsync<T>(FilterDefinition<T> filter, CancellationToken cancellation = default)
            where T : IEntityBase
        {
            return DB.CountAsync(filter, this, cancellation);
        }

        /// <summary>
        /// Gets an accurate count of how many total entities are in the collection for a given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The entity type to get the count for</typeparam>
        /// <param name="filter">f => f.Eq(x => x.Prop, Value) &amp; f.Gt(x => x.Prop, Value)</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> CountAsync<T>(Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> filter,
            CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.CountAsync(filter, this, cancellation);
        }

        /// <summary>
        /// Starts an update command for the given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        public virtual Update<T> Update<T>() where T : IEntityBase
        {
            return new Update<T>(session);
        }

        /// <summary>
        /// Starts a find command for the given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        [Obsolete("使用Queryable")]
        public virtual Find<T> Find<T>() where T : IEntityBase
        {
            return new Find<T>(this);
        }

        /// <summary>
        /// Starts a find command with projection support for the given entity type in the transaction scope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TProjection">The type of the end result</typeparam>
        public virtual Find<T, TProjection> Find<T, TProjection>() where T : IEntityBase
        {
            return new Find<T, TProjection>(this);
        }

        /// <summary>
        /// Exposes the MongoDB collection for the given entity type as IAggregateFluent in order to facilitate Fluent queries in the transaction sope.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        public virtual IAggregateFluent<T> Fluent<T>(AggregateOptions options = null) where T : IEntityBase
        {
            return DB.Fluent<T>(options, session);
        }

        /// <summary>
        /// Exposes the MongoDB collection for the given entity type as IQueryable in order to facilitate LINQ queries in the transaction scope.
        /// </summary>
        /// <param name="options">The aggregate options</param>
        /// <typeparam name="T">The type of entity</typeparam>
        public virtual IQueryable<T> Query<T>(AggregateOptions options = null) where T : IEntityBase
        {
            var entityType = typeof(T).GetRootBsonClassMap().ClassType;
            if (typeof(T).IsInterface)
            {
                return (IQueryable<T>)this.Query(entityType);
            }

            var query = this.Query(entityType);
            if (entityType != typeof(T))
            {
                query = query.OfType<T>();
            }
            return (IQueryable<T>)query;
        }

        /// <summary>
        /// Exposes the MongoDB collection for the given entity type as IQueryable in order to facilitate LINQ queries in the transaction scope.
        /// </summary>
        /// <param name="options">The aggregate options</param>
        public virtual IQueryable Query(Type entityType, AggregateOptions options = null)
        {
            if (entityType.IsInterface)
            {
                return this.Query(entityType.GetRootBsonClassMap().ClassType);
            }
            return typeof(DB).GetMethod(nameof(DB.Queryable)).MakeGenericMethod(entityType).Invoke(null, new object[] { options, this }).As<IQueryable>();
        }

        public virtual IMongoCollection<T> Collection<T>() where T : IEntityBase
        {
            return DB.Collection<T>();
        }

        /// <summary>
        /// Executes an aggregation pipeline in the transaction scope by supplying a 'Template' object.
        /// Gets a cursor back as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<IAsyncCursor<TResult>> PipelineCursorAsync<T, TResult>(TemplateQuery<T, TResult> template,
            AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.PipelineCursorAsync(template, options, session, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline in the transaction scope by supplying a 'Template' object.
        /// Gets a list back as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<List<TResult>> PipelineAsync<T, TResult>(TemplateQuery<T, TResult> template,
            AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.PipelineAsync(template, options, session, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline in the transaction scope by supplying a 'Template' object.
        /// Gets a single or default value as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<TResult> PipelineSingleAsync<T, TResult>(TemplateQuery<T, TResult> template,
            AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.PipelineSingleAsync(template, options, session, cancellation);
        }

        /// <summary>
        /// Executes an aggregation pipeline in the transaction scope by supplying a 'Template' object.
        /// Gets the first or default value as the result.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <typeparam name="TResult">The type of the resulting objects</typeparam>
        /// <param name="template">A 'Template' object with tags replaced</param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<TResult> PipelineFirstAsync<T, TResult>(TemplateQuery<T, TResult> template,
            AggregateOptions options = null, CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.PipelineFirstAsync(template, options, session, cancellation);
        }

        /// <summary>
        /// Start a fluent aggregation pipeline with a $GeoNear stage with the supplied parameters in the transaction scope.
        /// </summary>
        /// <param name="NearCoordinates">The coordinates from which to find documents from</param>
        /// <param name="DistanceField">x => x.Distance</param>
        /// <param name="Spherical">Calculate distances using spherical geometry or not</param>
        /// <param name="MaxDistance">The maximum distance in meters from the center point that the documents can be</param>
        /// <param name="MinDistance">The minimum distance in meters from the center point that the documents can be</param>
        /// <param name="Limit">The maximum number of documents to return</param>
        /// <param name="Query">Limits the results to the documents that match the query</param>
        /// <param name="DistanceMultiplier">The factor to multiply all distances returned by the query</param>
        /// <param name="IncludeLocations">Specify the output field to store the point used to calculate the distance</param>
        /// <param name="IndexKey"></param>
        /// <param name="options">The options for the aggregation. This is not required.</param>
        /// <typeparam name="T">The type of entity</typeparam>
        public virtual IAggregateFluent<T> GeoNear<T>(Coordinates2D NearCoordinates,
            Expression<Func<T, object>> DistanceField, bool Spherical = true, int? MaxDistance = null,
            int? MinDistance = null, int? Limit = null, BsonDocument Query = null, int? DistanceMultiplier = null,
            Expression<Func<T, object>> IncludeLocations = null, string IndexKey = null,
            AggregateOptions options = null) where T : IEntityBase
        {
            return DB.FluentGeoNear(NearCoordinates, DistanceField, Spherical, MaxDistance, MinDistance, Limit, Query,
                DistanceMultiplier, IncludeLocations, IndexKey, options, session);
        }

        /// <summary>
        /// Saves an entity partially with only the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<UpdateResult> SaveOnlyAsync<T>(T entity, Expression<Func<T, object>> members,
            CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.SaveOnlyAsync(entity, members, this, cancellation);
        }

        /// <summary>
        /// Saves a batch of entities partially with only the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be saved can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<BulkWriteResult<T>> SaveOnlyAsync<T>(IEnumerable<T> entities,
            Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.SaveOnlyAsync(entities, members, this, cancellation);
        }

        /// <summary>
        /// Saves an entity partially excluding the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<UpdateResult> SaveExceptAsync<T>(T entity, Expression<Func<T, object>> members,
            CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.SaveExceptAsync(entity, members, this, cancellation);
        }

        /// <summary>
        /// Saves a batch of entities partially excluding the specified subset of properties.
        /// If Id value is null, a new entity is created. If Id has a value, then existing entity is updated.
        /// <para>TIP: The properties to be excluded can be specified with a 'New' expression.
        /// You can only specify root level properties with the expression.</para>
        /// </summary>
        /// <typeparam name="T">Any class that implements IEntity</typeparam>
        /// <param name="entities">The batch of entities to save</param>
        /// <param name="members">x => new { x.PropOne, x.PropTwo }</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<BulkWriteResult<T>> SaveExceptAsync<T>(IEnumerable<T> entities,
            Expression<Func<T, object>> members, CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.SaveExceptAsync(entities, members, this, cancellation);
        }

        /// <summary>
        /// Saves an entity partially while excluding some properties in the transaction scope.
        /// The properties to be excluded can be specified using the [Preserve] or [DontPreserve] attributes.
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="entity">The entity to save</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<UpdateResult> SavePreservingAsync<T>(T entity, CancellationToken cancellation = default)
            where T : IEntityBase
        {
            return DB.SavePreservingAsync(entity, session, cancellation);
        }

        /// <summary>
        /// Deletes a single entity from MongoDB in the transaction scope.
        /// <para>HINT: If this entity is referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="id">The Id of the entity to delete</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> DeleteAsync<T>(string id, CancellationToken cancellation = default)
            where T : IEntityBase
        {
            return DB.DeleteAsync<T>(id, this, cancellation);
        }

        public virtual Task<long> DeleteAsync<T>(T entity, CancellationToken cancellation = default)
            where T : IEntityBase
        {
            return DB.DeleteAsync(entity.GetType(), entity.Id, this, cancellation);
        }

        /// <summary>
        /// Bulk delete entities without any tracking
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="expression">A lambda expression for matching entities to delete.</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> DeleteAsync<T>(Expression<Func<T, bool>> expression,
            CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.DeleteAsync(expression, this, cancellation);
        }
        /// <summary>
        /// Bulk delete entities without any tracking
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public virtual Task<long> DeleteAsync<T>(
            CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.DeleteAsync<T>(this, cancellation);
        }

        /// <summary>
        /// Deletes matching entities from MongoDB in the transaction scope
        /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
        /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="Ids">An IEnumerable of entity Ids</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public virtual Task<long> DeleteAsync<T>(IEnumerable<string> Ids,
            CancellationToken cancellation = default) where T : IEntityBase
        {
            return DB.DeleteAsync<T>(Ids, this, cancellation);
        }

        /// <summary>
        /// Start a fluent aggregation pipeline with a $text stage with the supplied parameters in the transaction scope.
        /// <para>TIP: Make sure to define a text index with DB.Index&lt;T&gt;() before searching</para>
        /// </summary>
        /// <param name="findSearchType">The type of text matching to do</param>
        /// <param name="searchTerm">The search term</param>
        /// <param name="caseSensitive">Case sensitivity of the search (optional)</param>
        /// <param name="diacriticSensitive">Diacritic sensitivity of the search (optional)</param>
        /// <param name="language">The language for the search (optional)</param>
        /// <param name="options">Options for finding documents (not required)</param>
        public virtual IAggregateFluent<T> FluentTextSearch<T>(FindSearchType findSearchType, string searchTerm,
            bool caseSensitive = false, bool diacriticSensitive = false, string language = null,
            AggregateOptions options = null) where T : IEntityBase
        {
            return DB.FluentTextSearch<T>(findSearchType, searchTerm, caseSensitive, diacriticSensitive, language, options,
                session);
        }

        protected static MethodInfo saveMethod = typeof(Extensions).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).First(x => x.Name == nameof(Extensions.SaveAsync) && x.GetParameters().First().ParameterType.Name.Contains("IEnumerable"));
        private static MethodInfo castMethod = typeof(Enumerable).GetMethods().First(x => x.Name == nameof(Enumerable.Cast) && x.GetParameters().First().ParameterType == typeof(IEnumerable));

        public virtual async Task<List<string>> SaveChanges(CancellationToken cancellation = default)
        {
            if (this.PreSaveChanges != default)
            {
                await this.PreSaveChanges();
            }
            var savedIds = new List<string>();
            foreach (var (type, keyedEntities) in this.MemoryDataCache.TypedCacheDictionary)
            {
                if (!keyedEntities.Any())
                {
                    continue;
                }
                var toSavedEntities = new List<IEntityBase>();
                foreach (var (key, value) in keyedEntities)
                {
                    // 如果值没有改变, 则不保存
                    var newValue = this.MemoryDataCache[type][key];
                    if (this.DbDataCache[type].TryGetValue(key, out var originValue) && this.EntityChangeSet(newValue, originValue).AreEqual)
                    {
                        continue;
                    }
                    toSavedEntities.Add(newValue);
                }
                // 如果本地有值变更
                if (toSavedEntities.Count != 0)
                {
                    const int maxRetries = 3;
                    for (int attempt = 0; attempt < maxRetries; attempt++)
                    {
                        try
                        {
                            savedIds.AddRange(toSavedEntities.Select(x => $"{type.Name}@{x.Id}"));
                            var list = (castMethod.MakeGenericMethod(type).Invoke(null, new object[] { toSavedEntities }));
                            if (!Session.IsInTransaction && this.SupportTransaction)
                            {
                                Session.StartTransaction();
                            }
                            var saveTask = saveMethod.MakeGenericMethod(type)
                                .Invoke(null, new object[] { list, this.Session, cancellation });
                            if (saveTask is Task task)
                            {
                                await task.ConfigureAwait(false);
                            }

                            if (Session.IsInTransaction)
                            {
                                await Session.CommitTransactionAsync(cancellation).ConfigureAwait(false);
                            }
                            break; // 如果成功，跳出循环
                        }
                        catch (MongoException ex) when (ex.HasErrorLabel("TransientTransactionError"))
                        {
                            if (attempt == maxRetries - 1)
                                throw; // 如果是最后一次尝试，则抛出异常

                            await Task.Delay(TimeSpan.FromSeconds(0.5 * (attempt + 1)), cancellation); // 指数退避
                        }
                    }
                }
            }

            //this.Local.TypedCacheDictionary.Clear();
            this.DbDataCache.TypedCacheDictionary.Clear();
            if (this.PostSaveChanges != default)
            {
                await this.PostSaveChanges();
            }
            return savedIds;
        }
        /// <summary>
        /// 检查value是否发生变更
        /// </summary>
        /// <param name="newValue"></param>
        /// <param name="originValue"></param>
        /// <returns></returns>
        protected virtual ComparisonResult EntityChangeSet(IEntityBase newValue, IEntityBase originValue)
        {
            return _compareLogic.Compare(newValue, originValue);
        }

        #region IDisposable Support

        private bool disposedValue;
        protected static CompareLogic _compareLogic = new CompareLogic(new ComparisonConfig()
        {
            CompareFields = false,
            CompareReadOnly = false,
            CompareStaticFields = false,
            CompareStaticProperties = false,
            MaxStructDepth = 5,
            Caching = true,
            AutoClearCache = false,
            IgnoreLogicGetters = true,
            MembersToIgnore = new List<string>()
            {
                nameof(IModifiedOn.ModifiedOn)
            },
        });

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    MemoryDataCache.TypedCacheDictionary.Clear();
                    DbDataCache.TypedCacheDictionary.Clear();
                    if (session.IsInTransaction)
                    {
                        session.AbortTransaction();
                    }
                    session.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        public static ClientSessionOptions DefaultSessionOptions = new ClientSessionOptions
        {
            CausalConsistency = true,
            DefaultTransactionOptions = new TransactionOptions(
                            readPreference: ReadPreference.Primary,
                            readConcern: ReadConcern.Local,
                            writeConcern: WriteConcern.Acknowledged,
                            maxCommitTime: new Optional<TimeSpan?>(TimeSpan.FromSeconds(600))),
        };

        public void InsertOne<T>(T entity,
            InsertOneOptions? options = default, CancellationToken cancellationToken = default) where T : IEntityBase
        {
            this.Collection<T>().InsertOne(this.Session, entity, options, cancellationToken);
        }
        public void InsertMany<T>(IEnumerable<T> entities,
            InsertManyOptions? options = default, CancellationToken cancellationToken = default) where T : IEntityBase
        {
            this.Collection<T>().InsertMany(this.Session, entities, options, cancellationToken);
        }
        public Task InsertOneAsync<T>(T entity,
            InsertOneOptions? options = default, CancellationToken cancellationToken = default) where T : IEntityBase
        {
            return this.Collection<T>().InsertOneAsync(this.Session, entity, options, cancellationToken);
        }
        public Task InsertManyAsync<T>(IEnumerable<T> entities,
            InsertManyOptions? options = default, CancellationToken cancellationToken = default) where T : IEntityBase
        {
            return this.Collection<T>().InsertManyAsync(this.Session, entities, options, cancellationToken);
        }

        public virtual event Func<Task>? PreSaveChanges;
        public virtual event Func<Task>? PostSaveChanges;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{this.GetHashCode()}:{this.session?.GetHashCode()}";
        }

        internal void ThrowIfCancellationNotSupported(CancellationToken cancellation = default)
        {
            if (cancellation != default && this?.Session == null)
                throw new NotSupportedException("Cancellation is only supported within transactions for delete operations!");
        }

        public void UpdateDbDataCache<T>(T dbEntity, bool force = false) where T : IEntityBase
        {
            var rootType = typeof(T).GetRootBsonClassMap().ClassType;
            UpdateDbDataCache(dbEntity, rootType, force);
        }

        public void UpdateDbDataCache<T>(IEnumerable<T> dbEntities, bool force = false) where T : IEntityBase
        {
            if (!dbEntities.Any())
            {
                return;
            }
            var rootType = typeof(T).GetRootBsonClassMap().ClassType;
            foreach (var dbEntity in dbEntities)
            {
                UpdateDbDataCache(dbEntity, rootType, force);
            }
        }

        private void UpdateDbDataCache<T>(T dbEntity, Type rootType, bool force = false) where T : IEntityBase
        {
            if (force)
            {
                var tobeCached = dbEntity.ShallowClone();
                tobeCached = dbEntity.Adapt(tobeCached);
                this.DbDataCache[rootType].AddOrUpdate(dbEntity.Id, tobeCached, (_, _) => tobeCached);
                return;
            }
            if (!DbDataCache[rootType].ContainsKey(dbEntity.Id))
            {
                // todo: Copy dbEntity and save it to OriginLocal
                var tobeCached = dbEntity.ShallowClone();
                tobeCached = dbEntity.Adapt(tobeCached);
                this.DbDataCache[rootType].TryAdd(dbEntity.Id, dbEntity.Adapt(tobeCached));
            }
        }

        /// <summary>
        /// 取消特定对象的对象跟踪,
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        public void Detach<T>(T entity) where T : IEntityBase
        {
            ArgumentNullException.ThrowIfNull(entity);
            var entityType = entity.GetType();
            if (entity.DbContext != this)
            {
                ServiceProvider.GetService<ILogger<DbContext>>()?.LogWarning("Detaching {EntityType} with Id {Id} from an ambiguous DbContext: entity.DbContext={entity.DbContext} but current={currentDbContext}", entityType.Name, entity.Id, entity.DbContext?.GetHashCode(), this?.GetHashCode());
            }
            entity.DbContext = null;
            var rootType = entityType.GetRootBsonClassMap().ClassType;
            this.MemoryDataCache[rootType].TryRemove(entity.Id, out _);
            this.DbDataCache[rootType].TryRemove(entity.Id, out _);
        }

        /// <summary>
        /// 取消特定对象的对象跟踪,
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities"></param>
        public void Detach<T>(IEnumerable<T> entities) where T : IEntityBase
        {
            ArgumentNullException.ThrowIfNull(entities);
            var array = entities.ToArray();
            if (array.Length == 0)
            {
                return;
            }
            var rootType = array.First().GetType().GetRootBsonClassMap().ClassType;
            foreach (var entity in array)
            {
                ArgumentNullException.ThrowIfNull(entity);
                entity.DbContext = null;
                this.MemoryDataCache[rootType].TryRemove(entity.Id, out _);
                this.DbDataCache[rootType].TryRemove(entity.Id, out _);
            }
        }
    }
}
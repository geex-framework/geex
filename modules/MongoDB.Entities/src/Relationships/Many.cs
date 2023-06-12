using MongoDB.Driver;
using MongoDB.Driver.Linq;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Entities
{
    /// <summary>
    /// Base class providing shared state for Many'1 classes
    /// </summary>
    public abstract class ManyBase
    {
        //shared state for all Many<T> instances
        internal static ConcurrentBag<string> indexedCollections = new ConcurrentBag<string>();
        internal static string PropType = typeof(Many<EntityBase<IEntityBase>>).Name;
    }

    /// <summary>
    /// Represents a one-to-many/many-to-many relationship between two Entities.
    /// <para>WARNING: You have to initialize all instances of this class before accessing any of it's members.</para>
    /// <para>Initialize from the constructor of the parent entity as follows:</para>
    /// <c>this.InitOneToMany(() => Property)</c>
    /// <c>this.InitManyToMany(() => Property, x => x.OtherProperty)</c>
    /// </summary>
    /// <typeparam name="TChild">Type of the child IEntity.</typeparam>
    public class Many<TChild> : ManyBase, IEnumerable<TChild> where TChild : IEntityBase
    {
        private static readonly BulkWriteOptions unOrdBlkOpts = new BulkWriteOptions { IsOrdered = false };

        public DbContext DbContext { get; }
        public bool isInverse;
        public IEntityBase parent;

        /// <inheritdoc/>
        public IEnumerator<TChild> GetEnumerator() => ChildrenQueryable().GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => ChildrenQueryable().GetEnumerator();

        /// <summary>
        /// Gets the IMongoCollection of JoinRecords for this relationship.
        /// <para>TIP: Try never to use this unless really neccessary.</para>
        /// </summary>
        public IMongoCollection<JoinRecord> JoinCollection { get; private set; }

        /// <summary>
        /// An IQueryable of JoinRecords for this relationship
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<JoinRecord> JoinQueryable(DbContext replaceContext = null, AggregateOptions options = null)
        {

            replaceContext ??= DbContext;
            return replaceContext == null
                   ? JoinCollection.AsQueryable(options)
                   : JoinCollection.AsQueryable(replaceContext.Session, options);
        }

        /// <summary>
        /// An IAggregateFluent of JoinRecords for this relationship
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IAggregateFluent<JoinRecord> JoinFluent(DbContext replaceContext = null, AggregateOptions options = null)
        {
            replaceContext ??= DbContext;
            return replaceContext == null
                ? JoinCollection.Aggregate(options)
                : JoinCollection.Aggregate(replaceContext.Session, options);
        }

        /// <summary>
        /// Get an IQueryable of parents matching a single child Id for this relationship.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
        /// <param name="childId">A child Id</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<TParent> ParentsQueryable<TParent>(string childId, DbContext replaceContext = null, AggregateOptions options = null) where TParent : IEntityBase
        {
            return ParentsQueryable<TParent>(new[] { childId }, replaceContext ?? this.DbContext, options);
        }

        public IMongoQueryable<TParent> ParentsQueryable<TParent>(IQueryable<string> childIds,
            DbContext replaceContext = null, AggregateOptions options = null) where TParent : IEntityBase
        {
            return this.ParentsQueryable<TParent>(childIds.ToList());
        }

        /// <summary>
        /// Get an IQueryable of parents matching multiple child Ids for this relationship.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
        /// <param name="childIds">An IEnumerable of child Ids</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<TParent> ParentsQueryable<TParent>(IEnumerable<string> childIds, DbContext replaceContext = null, AggregateOptions options = null) where TParent : IEntityBase
        {
            replaceContext ??= this.DbContext;
            if (typeof(TParent) == typeof(TChild)) throw new InvalidOperationException("Both parent and child types cannot be the same");

            if (isInverse)
            {
                return JoinQueryable(replaceContext, options)
                       .Where(j => childIds.Contains(j.ParentId))
                       .Join(
                           DB.Collection<TParent>(),
                           j => j.ChildId,
                           p => p.Id,
                           (_, p) => p)
                       .Distinct();
            }
            else
            {
                return JoinQueryable(replaceContext, options)
                       .Where(j => childIds.Contains(j.ChildId))
                       .Join(
                           DB.Collection<TParent>(),
                           j => j.ParentId,
                           p => p.Id,
                           (_, p) => p)
                       .Distinct();
            }
        }

        /// <summary>
        /// Get an IQueryable of parents matching a supplied IQueryable of children for this relationship.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
        /// <param name="children">An IQueryable of children</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<TParent> ParentsQueryable<TParent>(IMongoQueryable<TChild> children, DbContext replaceContext = null, AggregateOptions options = null) where TParent : IEntityBase
        {
            replaceContext ??= this.DbContext;
            if (typeof(TParent) == typeof(TChild)) throw new InvalidOperationException("Both parent and child types cannot be the same");

            if (isInverse)
            {
                return children
                        .Join(
                             JoinQueryable(replaceContext, options),
                             c => c.Id,
                             j => j.ParentId,
                             (_, j) => j)
                        .Join(
                           DB.Collection<TParent>(),
                           j => j.ChildId,
                           p => p.Id,
                           (_, p) => p)
                        .Distinct();
            }
            else
            {
                return children
                       .Join(
                            JoinQueryable(replaceContext, options),
                            c => c.Id,
                            j => j.ChildId,
                            (_, j) => j)
                       .Join(
                            DB.Collection<TParent>(),
                            j => j.ParentId,
                            p => p.Id,
                            (_, p) => p)
                       .Distinct();
            }
        }

        /// <summary>
        /// Get an IAggregateFluent of parents matching a supplied IAggregateFluent of children for this relationship.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
        /// <param name="children">An IAggregateFluent of children</param>
        public IAggregateFluent<TParent> ParentsFluent<TParent>(IAggregateFluent<TChild> children) where TParent : IEntityBase
        {
            if (typeof(TParent) == typeof(TChild)) throw new InvalidOperationException("Both parent and child types cannot be the same");

            if (isInverse)
            {
                return children
                       .Lookup<TChild, JoinRecord, Joined<JoinRecord>>(
                            JoinCollection,
                            c => c.Id,
                            r => r.ParentId,
                            j => j.Results)
                       .ReplaceRoot(j => j.Results[0])
                       .Lookup<JoinRecord, TParent, Joined<TParent>>(
                            DB.Collection<TParent>(),
                            r => r.ChildId,
                            p => p.Id,
                            j => j.Results)
                       .ReplaceRoot(j => j.Results[0])
                       .Distinct();
            }
            else
            {
                return children
                       .Lookup<TChild, JoinRecord, Joined<JoinRecord>>(
                            JoinCollection,
                            c => c.Id,
                            r => r.ChildId,
                            j => j.Results)
                       .ReplaceRoot(j => j.Results[0])
                       .Lookup<JoinRecord, TParent, Joined<TParent>>(
                            DB.Collection<TParent>(),
                            r => r.ParentId,
                            p => p.Id,
                            j => j.Results)
                       .ReplaceRoot(j => j.Results[0])
                       .Distinct();
            }
        }

        /// <summary>
        /// Get an IAggregateFluent of parents matching a single child Id for this relationship.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
        /// <param name="childId">An child Id</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IAggregateFluent<TParent> ParentsFluent<TParent>(string childId, DbContext replaceContext = null, AggregateOptions options = null) where TParent : IEntityBase
        {
            return ParentsFluent<TParent>(new[] { childId }, replaceContext ?? this.DbContext, options);
        }

        /// <summary>
        /// Get an IAggregateFluent of parents matching multiple child Ids for this relationship.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent IEntity</typeparam>
        /// <param name="childIds">An IEnumerable of child Ids</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IAggregateFluent<TParent> ParentsFluent<TParent>(IEnumerable<string> childIds, DbContext replaceContext = null, AggregateOptions options = null) where TParent : IEntityBase
        {
            replaceContext ??= this.DbContext;
            if (typeof(TParent) == typeof(TChild)) throw new InvalidOperationException("Both parent and child types cannot be the same");

            if (isInverse)
            {
                return JoinFluent(replaceContext, options)
                       .Match(f => f.In(j => j.ParentId, childIds))
                       .Lookup<JoinRecord, TParent, Joined<TParent>>(
                            DB.Collection<TParent>(),
                            j => j.ChildId,
                            p => p.Id,
                            j => j.Results)
                       .ReplaceRoot(j => j.Results[0])
                       .Distinct();
            }
            else
            {
                return JoinFluent(replaceContext, options)
                       .Match(f => f.In(j => j.ChildId, childIds))
                       .Lookup<JoinRecord, TParent, Joined<TParent>>(
                            DB.Collection<TParent>(),
                            r => r.ParentId,
                            p => p.Id,
                            j => j.Results)
                       .ReplaceRoot(j => j.Results[0])
                       .Distinct();
            }
        }

        /// <summary>
        /// Get the number of children for a relationship
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task<long> ChildrenCountAsync(DbContext replaceContext = null, CountOptions options = null, CancellationToken cancellation = default)
        {
            replaceContext ??= this.DbContext;
            parent.ThrowIfUnsaved();

            if (isInverse)
            {
                return replaceContext == null
                       ? JoinCollection.CountDocumentsAsync(j => j.ChildId == parent.Id, options, cancellation)
                       : JoinCollection.CountDocumentsAsync(replaceContext.Session, j => j.ChildId == parent.Id, options, cancellation);
            }
            else
            {
                return replaceContext == null
                       ? JoinCollection.CountDocumentsAsync(j => j.ParentId == parent.Id, options, cancellation)
                       : JoinCollection.CountDocumentsAsync(replaceContext.Session, j => j.ParentId == parent.Id, options, cancellation);
            }
        }

        /// <summary>
        /// An IQueryable of child Entities for the parent.
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IMongoQueryable<TChild> ChildrenQueryable(DbContext replaceContext = null, AggregateOptions options = null)
        {
            replaceContext ??= this.DbContext;
            parent.ThrowIfUnsaved();

            if (isInverse)
            {
                return JoinQueryable(replaceContext, options)
                       .Where(j => j.ChildId == parent.Id)
                       .Join(
                           DB.Collection<TChild>(),
                           j => j.ParentId,
                           c => c.Id,
                           (_, c) => c);
            }
            else
            {
                return JoinQueryable(replaceContext, options)
                       .Where(j => j.ParentId == parent.Id)
                       .Join(
                           DB.Collection<TChild>(),
                           j => j.ChildId,
                           c => c.Id,
                           (_, c) => c);
            }
        }

        /// <summary>
        /// An IAggregateFluent of child Entities for the parent.
        /// </summary>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="options">An optional AggregateOptions object</param>
        public IAggregateFluent<TChild> ChildrenFluent(DbContext replaceContext = null, AggregateOptions options = null)
        {
            replaceContext ??= this.DbContext;
            parent.ThrowIfUnsaved();

            if (isInverse)
            {
                return JoinFluent(replaceContext, options)
                        .Match(f => f.Eq(r => r.ChildId, parent.Id))
                        .Lookup<JoinRecord, TChild, Joined<TChild>>(
                            DB.Collection<TChild>(),
                            r => r.ParentId,
                            c => c.Id,
                            j => j.Results)
                        .ReplaceRoot(j => j.Results[0]);
            }
            else
            {
                return JoinFluent(replaceContext, options)
                        .Match(f => f.Eq(r => r.ParentId, parent.Id))
                        .Lookup<JoinRecord, TChild, Joined<TChild>>(
                            DB.Collection<TChild>(),
                            r => r.ChildId,
                            c => c.Id,
                            j => j.Results)
                        .ReplaceRoot(j => j.Results[0]);
            }
        }

        internal Many(DbContext dbContext)
        {
            this.DbContext = dbContext;
            throw new InvalidOperationException("Parameterless constructor is disabled!");
        }

        internal Many(IEntityBase parent, string property)
        {
            this.DbContext = parent.DbContext;
            Init((dynamic)parent, property);
        }

        private void Init<TParent>(TParent parent, string property) where TParent : IEntityBase
        {
            if (DB.DatabaseName<TParent>() != DB.DatabaseName<TChild>())
                throw new NotSupportedException("Cross database relationships are not supported!");

            this.parent = parent;
            isInverse = false;
            JoinCollection = DB.GetRefCollection<TParent>($"[{DB.CollectionName<TParent>()}~{DB.CollectionName<TChild>()}({property})]");
            CreateIndexesAsync(JoinCollection);
        }

        internal Many(IEntityBase parent, string propertyParent, string propertyChild, bool isInverse)
        {
            this.DbContext = parent.DbContext;
            Init((dynamic)parent, propertyParent, propertyChild, isInverse);
        }

        private void Init<TParent>(TParent parent, string propertyParent, string propertyChild, bool isInverse) where TParent : IEntityBase
        {
            this.parent = parent;
            this.isInverse = isInverse;

            if (this.isInverse)
            {
                JoinCollection = DB.GetRefCollection<TParent>($"[({propertyParent}){DB.CollectionName<TChild>()}~{DB.CollectionName<TParent>()}({propertyChild})]");
            }
            else
            {
                JoinCollection = DB.GetRefCollection<TParent>($"[({propertyChild}){DB.CollectionName<TParent>()}~{DB.CollectionName<TChild>()}({propertyParent})]");
            }

            CreateIndexesAsync(JoinCollection);
        }

        private static Task CreateIndexesAsync(IMongoCollection<JoinRecord> collection)
        {
            //only create indexes once (best effort) per unique ref collection
            if (!indexedCollections.Contains(collection.CollectionNamespace.CollectionName))
            {
                indexedCollections.Add(collection.CollectionNamespace.CollectionName);
                collection.Indexes.CreateManyAsync(
                    new[] {
                        new CreateIndexModel<JoinRecord>(
                            Builders<JoinRecord>.IndexKeys.Ascending(r => r.ParentId),
                            new CreateIndexOptions
                            {
                                Background = true,
                                Name = "[ParentId]"
                            })
                        ,
                        new CreateIndexModel<JoinRecord>(
                            Builders<JoinRecord>.IndexKeys.Ascending(r => r.ChildId),
                            new CreateIndexOptions
                            {
                                Background = true,
                                Name = "[ChildId]"
                            })
                    });
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds a new child reference.
        /// <para>WARNING: Make sure to save the parent and child Entities before calling this method.</para>
        /// </summary>
        /// <param name="child">The child Entity to add.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task AddAsync(TChild child, DbContext replaceContext = null, CancellationToken cancellation = default)
        {
            replaceContext ??= DbContext;
            return AddAsync(child.Id, replaceContext, cancellation);
        }

        /// <summary>
        /// Adds multiple child references in a single bulk operation
        /// <para>WARNING: Make sure to save the parent and child Entities before calling this method.</para>
        /// </summary>
        /// <param name="children">The child Entities to add</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task AddAsync(IEnumerable<TChild> children, DbContext replaceContext = null, CancellationToken cancellation = default)
        {
            replaceContext ??= DbContext;
            replaceContext?.Attach((IEnumerable<IEntityBase>)children);
            return AddAsync(children.Select(c => c.Id), replaceContext, cancellation);
        }


        /// <summary>
        /// Adds a new child reference.
        /// <para>WARNING: Make sure to save the parent and child Entities before calling this method.</para>
        /// </summary>
        /// <param name="childId">The Id of the child Entity to add.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task AddAsync(string childId, DbContext replaceContext = null, CancellationToken cancellation = default)
        {
            replaceContext ??= this.DbContext;
            return AddAsync(new[] { childId }, replaceContext, cancellation);
        }

        /// <summary>
        /// Adds multiple child references in a single bulk operation
        /// <para>WARNING: Make sure to save the parent and child Entities before calling this method.</para>
        /// </summary>
        /// <param name="childIds">The Ids of the child Entities to add.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task AddAsync(IEnumerable<string> childIds, DbContext replaceContext = null, CancellationToken cancellation = default)
        {
            replaceContext ??= this.DbContext;
            parent.ThrowIfUnsaved();

            var models = new List<WriteModel<JoinRecord>>();
            foreach (var cid in childIds)
            {
                cid.ThrowIfUnsaved();

                var parentId = isInverse ? cid : parent.Id;
                var childId = isInverse ? parent.Id : cid;

                var filter = Builders<JoinRecord>.Filter.Where(
                    j => j.ParentId == parentId &&
                    j.ChildId == childId);

                var update = Builders<JoinRecord>.Update
                    .Set(j => j.ParentId, parentId)
                    .Set(j => j.ChildId, childId);

                models.Add(new UpdateOneModel<JoinRecord>(filter, update) { IsUpsert = true });
            }

            return replaceContext == null
                   ? JoinCollection.BulkWriteAsync(models, unOrdBlkOpts, cancellation)
                   : JoinCollection.BulkWriteAsync(replaceContext.Session, models, unOrdBlkOpts, cancellation);
        }

        /// <summary>
        /// Removes a child reference.
        /// </summary>
        /// <param name="child">The child IEntity to remove the reference of.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task RemoveAsync(TChild child, DbContext replaceContext = null, CancellationToken cancellation = default)
        {
            return RemoveAsync(child.Id, replaceContext, cancellation);
        }

        /// <summary>
        /// Removes a child reference.
        /// </summary>
        /// <param name="childId">The Id of the child Entity to remove the reference of.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task RemoveAsync(string childId, DbContext replaceContext = null, CancellationToken cancellation = default)
        {
            return RemoveAsync(new[] { childId }, replaceContext, cancellation);
        }

        /// <summary>
        /// Removes child references.
        /// </summary>
        /// <param name="children">The child Entities to remove the references of.</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task RemoveAsync(IEnumerable<TChild> children, DbContext replaceContext = null, CancellationToken cancellation = default)
        {
            return RemoveAsync(children.Select(c => c.Id), replaceContext, cancellation);
        }

        /// <summary>
        /// Removes child references.
        /// </summary>
        /// <param name="childIds">The Ids of the child Entities to remove the references of</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name="cancellation">An optional cancellation token</param>
        public Task RemoveAsync(IEnumerable<string> childIds, DbContext replaceContext = null, CancellationToken cancellation = default)
        {
            replaceContext ??= this.DbContext;
            var filter =
                isInverse
                ? Builders<JoinRecord>.Filter.And(
                    Builders<JoinRecord>.Filter.Eq(j => j.ChildId, parent.Id),
                    Builders<JoinRecord>.Filter.In(j => j.ParentId, childIds))

                : Builders<JoinRecord>.Filter.And(
                    Builders<JoinRecord>.Filter.Eq(j => j.ParentId, parent.Id),
                    Builders<JoinRecord>.Filter.In(j => j.ChildId, childIds));

            return replaceContext == null
                   ? JoinCollection.DeleteOneAsync(filter, null, cancellation)
                   : JoinCollection.DeleteOneAsync(replaceContext.Session, filter, null, cancellation);
        }

        /// <summary>
        /// A class used to hold join results when joining relationships
        /// </summary>
        /// <typeparam name="T">The type of the resulting objects</typeparam>
        public class Joined<T> : JoinRecord
        {
            public T[] Results { get; set; }
        }
    }
}

using MongoDB.Driver;

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a one-to-one relationship with an IEntity.
    /// </summary>
    /// <typeparam name="T">Any type that implements IEntity</typeparam>
    public class One<T> where T : IEntityBase
    {
        /// <summary>
        /// The Id of the entity referenced by this instance.
        /// </summary>
        public string Id { get; set; }
        public DbContext DbContext { get; set; }

        /// <summary>
        /// Initializes a reference to an entity in MongoDB.
        /// </summary>
        /// <param name="entity">The actual entity this reference represents.</param>
        internal One(T entity)
        {
            entity.ThrowIfUnsaved();
            DbContext = entity.DbContext;
            Id = entity.Id;
        }

        /// <summary>
        /// Operator for returning a new One&lt;T&gt; object from an entity
        /// </summary>
        /// <param name="entity">The entity to make a reference to</param>
        public static implicit operator One<T>(T entity)
        {
            return new One<T>(entity);
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database.
        /// </summary>
        /// <param name="session">An optional session</param>
        /// <param name="cancellation">An optional cancellation token</param>
        /// <returns>A Task containing the actual entity</returns>
        public Task<T> ToEntityAsync(DbContext contextOverride = null, CancellationToken cancellation = default)
        {
            contextOverride ??= DbContext;
            return new Find<T>(contextOverride).OneAsync(Id, cancellation);
        }

        /// <summary>
        /// Fetches the actual entity this reference represents from the database with a projection.
        /// </summary>
        /// <param name="projection">x => new Test { PropName = x.Prop }</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name = "cancellation" > An optional cancellation token</param>
        /// <returns>A Task containing the actual projected entity</returns>
        public async Task<T> ToEntityAsync(Expression<Func<T, T>> projection, DbContext contextOverride = null, CancellationToken cancellation = default)
        {
            contextOverride ??= DbContext;
            return (await new Find<T>(contextOverride)
                        .Match(Id)
                        .Project(projection)
                        .ExecuteAsync(cancellation).ConfigureAwait(false))
                   .SingleOrDefault();
        }


        /// <summary>
        /// Fetches the actual entity this reference represents from the database with a projection.
        /// </summary>
        /// <param name="projection">p=> p.Include("Prop1").Exclude("Prop2")</param>
        /// <param name="session">An optional session if using within a transaction</param>
        /// <param name = "cancellation" > An optional cancellation token</param>
        /// <returns>A Task containing the actual projected entity</returns>
        public async Task<T> ToEntityAsync(Func<ProjectionDefinitionBuilder<T>, ProjectionDefinition<T, T>> projection, DbContext contextOverride = null, CancellationToken cancellation = default)
        {
            contextOverride ??= DbContext;
            return (await new Find<T>(contextOverride)
                        .Match(Id)
                        .Project(projection)
                        .ExecuteAsync(cancellation).ConfigureAwait(false))
                   .SingleOrDefault();
        }
    }
}

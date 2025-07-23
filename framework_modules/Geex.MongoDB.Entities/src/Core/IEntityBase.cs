using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mapster;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities.Utilities;

namespace MongoDB.Entities
{
    /// <summary>
    /// The contract for Entity classes
    /// </summary>
    public interface IEntityBase
    {
        /// <summary>
        /// The Id property for this entity type.
        /// 注意: dbcontext会根据entity是否有id值来判断当前entity是否为新增
        /// </summary>
        [ObjectId]
        string Id { get; set; }
        [AdaptIgnore]
        DbContext DbContext { get; set; }
        DateTimeOffset CreatedOn { get; set; }
        /// <summary>
        /// Generate and return a new Id string from this method. It will be used when saving new entities that don't have their Id set.
        /// That is, if an entity has a null Id, this method will be called for getting a new Id value.
        /// If you're not doing custom Id generation, simply do <c>return ObjectId.GenerateNewId().ToString()</c>
        /// </summary>
        ObjectId GenerateNewId();
        /// <summary>
        /// Deletes a single entity from MongoDB.
        /// </summary>
        virtual async Task<long> DeleteAsync()
        {
            return await DB.DeleteAsync(this.GetType(), this.Id, this.DbContext);
        }
        internal Dictionary<string, ILazyQuery> LazyQueryCache { get; }
        //protected internal ILazyQuery ConfigLazyQueryable(
        //   Expression lazyQuery,
        //   Expression batchQuery,
        //   Func<IQueryable> sourceProvider = default);
        public TChild Cast<TChild>() where TChild : IEntityBase;
    }
}
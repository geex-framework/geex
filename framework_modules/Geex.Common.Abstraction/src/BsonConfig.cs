using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Geex.Common.Abstraction.Storage;
using Microsoft.Extensions.DependencyInjection;

using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Entities;

using Volo.Abp.DependencyInjection;

namespace Geex.Common.Abstraction
{
    internal interface IBsonConfig
    {
        public void Map();
    }

    internal interface IBsonConfig<TEntity> : IBsonConfig where TEntity : IEntityBase
    {
        public BsonIndexConfig<TEntity> IndexConfig { get; }
        void IBsonConfig.Map()
        {
            var entityType = typeof(TEntity);
            var map = new BsonClassMap<TEntity>();
            this.Map(map);
            var noParamCtor = entityType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new Type[] { });
            if (noParamCtor != default)
            {
                map.MapConstructor(noParamCtor);
            }
            BsonClassMap.RegisterClassMap(map);
            //this.IndexConfig.Indexes.Where()
            //if (Cache<TEntity>.Indexes.All(x => x.ToString() != indexModel.Keys.Render(BsonSerializer.LookupSerializer<TEntity>(), BsonSerializer.SerializerRegistry)))
            //{

            //}

            if (this.IndexConfig.Indexes.Any() == true)
            {
                DB.Collection<TEntity>().Indexes.CreateMany(this.IndexConfig.Indexes, new CreateManyIndexesOptions()
                {
                    CommitQuorum = CreateIndexCommitQuorum.Majority,
                });
            }
        }
        public void Map(BsonClassMap<TEntity> map);
    }

    public class BsonIndexConfig<TEntity> where TEntity : IEntityBase
    {
        internal List<CreateIndexModel<TEntity>> Indexes { get; } = new List<CreateIndexModel<TEntity>>();

        public void MapIndex(Func<IndexKeysDefinitionBuilder<TEntity>, IndexKeysDefinition<TEntity>> keyFunc, Action<CreateIndexOptions<TEntity>>? action = default)
        {
            var createIndexOptions = new CreateIndexOptions<TEntity>();
            action?.Invoke(createIndexOptions);
            var indexModel = new CreateIndexModel<TEntity>(keyFunc(Builders<TEntity>.IndexKeys), createIndexOptions);
            this.Indexes.Add(indexModel);
        }
    }

    public static class BsonIndexConfigExtensions
    {
        public static void MapEntityDefaultIndex<TEntity>(this BsonIndexConfig<TEntity> @this) where TEntity : Entity<TEntity>
        {
            @this.MapIndex(builder => builder.Descending(x => x.CreatedOn));
            @this.MapIndex(builder => builder.Descending(x => x.ModifiedOn));
        }
    }

    [Dependency(ServiceLifetime.Transient)]
    [ExposeServices(typeof(IBsonConfig))]
    public abstract class BsonConfig<TEntity> : IBsonConfig<TEntity> where TEntity : IEntityBase
    {
        void IBsonConfig<TEntity>.Map(BsonClassMap<TEntity> map)
        {
            this.Map(map, IndexConfig);
        }

        public BsonIndexConfig<TEntity> IndexConfig { get; } = new BsonIndexConfig<TEntity>();

        protected abstract void Map(BsonClassMap<TEntity> map, BsonIndexConfig<TEntity> indexConfig);
    }
}

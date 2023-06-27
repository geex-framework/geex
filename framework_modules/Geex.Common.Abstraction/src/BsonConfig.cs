using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Bson.Serialization;
using MongoDB.Entities;

using Volo.Abp.DependencyInjection;

namespace Geex.Common.Abstraction
{
    public interface IBsonConfig
    {
        public void Map();
    }

    public interface IBsonConfig<TEntity> : IBsonConfig
    {
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
        }
        public void Map(BsonClassMap<TEntity> map);
    }


    [Dependency(ServiceLifetime.Transient)]
    [ExposeServices(typeof(IBsonConfig))]
    public abstract class BsonConfig<TEntity> : IBsonConfig<TEntity> where TEntity : IEntityBase
    {
        void IBsonConfig<TEntity>.Map(BsonClassMap<TEntity> map)
        {
            this.Map(map);
        }
        protected abstract void Map(BsonClassMap<TEntity> map);
    }
}

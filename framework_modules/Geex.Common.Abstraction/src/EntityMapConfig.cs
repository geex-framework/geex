using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Bson.Serialization;
using MongoDB.Entities;

using Volo.Abp.DependencyInjection;

using static Humanizer.On;

namespace Geex.Common.Abstraction
{
    [Dependency(ServiceLifetime.Transient)]
    [ExposeServices(typeof(IEntityMapConfig))]
    public abstract class EntityMapConfig<TEntity> : IEntityMapConfig where TEntity : IEntityBase
    {
        public abstract void Map(BsonClassMap<TEntity> map);

        /// <inheritdoc />
        void IEntityMapConfig.Map()
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
    }
    public interface IEntityMapConfig
    {
        public void Map();
    }
}

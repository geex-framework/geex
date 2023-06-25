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
    public interface IEntityBsonConfig
    {
        public void Map();
    }

    public interface IEntityBsonConfig<TEntity> : IEntityBsonConfig
    {
        void IEntityBsonConfig.Map()
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

    public interface IEntityGqlConfig
    {
        public void Configure(IDescriptor? descriptor);
    }

    public interface IEntityGqlConfig<TEntity> : IEntityGqlConfig
    {
        void IEntityGqlConfig.Configure(IDescriptor? descriptor)
        {
            if (descriptor is IObjectTypeDescriptor<TEntity> objectTypeDescriptor)
            {
                this.Configure(objectTypeDescriptor);
            }

            if (descriptor is IInterfaceTypeDescriptor<TEntity> interfaceTypeDescriptor)
            {
                this.Configure(interfaceTypeDescriptor);
            }
        }
        public void Configure(IObjectTypeDescriptor<TEntity> map);
    }


    [Dependency(ServiceLifetime.Transient)]
    [ExposeServices(typeof(IEntityBsonConfig), typeof(IEntityGqlConfig))]
    public abstract class EntityConfig<TEntity> : IEntityBsonConfig<TEntity>, IEntityGqlConfig<TEntity> where TEntity : IEntityBase
    {
        void IEntityBsonConfig<TEntity>.Map(BsonClassMap<TEntity> map)
        {
            this.Map(map);
        }
        protected abstract void Map(BsonClassMap<TEntity> map);

        void IEntityGqlConfig<TEntity>.Configure(IObjectTypeDescriptor<TEntity> descriptor)
        {
            this.Configure(descriptor);
        }

        protected abstract void Configure(IObjectTypeDescriptor<TEntity> descriptor);
    }
}

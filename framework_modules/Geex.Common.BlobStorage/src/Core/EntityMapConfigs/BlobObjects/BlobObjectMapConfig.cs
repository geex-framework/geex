using Geex.Common.BlobStorage.Core.Aggregates.BlobObjects;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Bson;
using Geex.Common.Abstraction.Entities;
using MongoDB.Bson.Serialization;
using MongoDB.Entities;
using Geex.Common.Abstraction.Storage;
using HotChocolate.Types;

namespace Geex.Common.BlobStorage.Core.EntityMapConfigs.BlobObjects
{
    public class BlobObjectEntityConfig : EntityConfig<BlobObject>
    {
        protected override void Map(BsonClassMap<BlobObject> map)
        {
            map.Inherit<IBlobObject>();
            map.AutoMap();
        }

        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<BlobObject> descriptor)
        {
            descriptor.BindFieldsImplicitly();
            descriptor.Implements<InterfaceType<IBlobObject>>();
            descriptor.ConfigEntity();
        }
    }
    public class DbFileEntityConfig : IEntityBsonConfig<DbFile>
    {
        public void Map(BsonClassMap<DbFile> map)
        {
            map.Inherit<FileEntity>();
            map.AutoMap();
        }
    }
}

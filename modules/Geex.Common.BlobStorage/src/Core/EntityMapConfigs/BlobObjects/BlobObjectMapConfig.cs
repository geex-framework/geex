using Geex.Common.BlobStorage.Core.Aggregates.BlobObjects;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Bson;
using Geex.Common.Abstraction.Entities;
using MongoDB.Bson.Serialization;
using MongoDB.Entities;

namespace Geex.Common.BlobStorage.Core.EntityMapConfigs.BlobObjects
{
    public class BlobObjectMapConfig : EntityMapConfig<BlobObject>
    {
        public override void Map(BsonClassMap<BlobObject> map)
        {
            map.Inherit<IBlobObject>();
            map.AutoMap();
        }

    }
    public class DbFileMapConfig : EntityMapConfig<DbFile>
    {
        public override void Map(BsonClassMap<DbFile> map)
        {
            map.Inherit<FileEntity>();
            map.AutoMap();
        }
    }
}

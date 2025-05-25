using Geex.Abstractions;
using MongoDB.Bson.Serialization;
using MongoDB.Entities;

namespace Geex.Extensions.BlobStorage.Aggregates.BlobObjects
{
    public class DbFile : FileEntity
    {
        public DbFile(string md5)
        {
            Md5 = md5;
        }

        public string Md5 { get; set; }

        public class DbFileEntityConfig : BsonConfig<DbFile>
        {
            /// <inheritdoc />
            protected override void Map(BsonClassMap<DbFile> map, BsonIndexConfig<DbFile> indexConfig)
            {
                map.Inherit<FileEntity>();
                map.AutoMap();
                indexConfig.MapIndex(builder => builder.Descending(x => x.CreatedOn));
                indexConfig.MapIndex(builder => builder.Hashed(x => x.Md5));
            }
        }
    }
}

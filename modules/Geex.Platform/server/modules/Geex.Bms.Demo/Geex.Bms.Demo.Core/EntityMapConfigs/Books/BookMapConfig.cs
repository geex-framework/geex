using Geex.Common.Abstraction;
using MongoDB.Bson.Serialization;
using Geex.Bms.Demo.Core.Aggregates.books;

namespace Geex.Bms.Demo.Core.EntityMapConfigs.books
{
    // * EntityMapConfigs：与MongoDb的映射，比如自定义一个字段的序列化方式或者别名等，比如Book可能有多个作者，我们在实体中是字符串，但是我们在数据库中存储为JSON。对比EF

    public class bookMapConfig : EntityMapConfig<Book>
    {
        public override void Map(BsonClassMap<Book> map)
        {
            map.SetIsRootClass(true);
            map.Inherit<Book>();
            map.AutoMap();
        }   
    }
}

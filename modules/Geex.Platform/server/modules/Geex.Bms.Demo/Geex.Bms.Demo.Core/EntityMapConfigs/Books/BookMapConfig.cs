using Geex.Common.Abstraction;
using MongoDB.Bson.Serialization;
using Geex.Bms.Demo.Core.Aggregates.books;

namespace Geex.Bms.Demo.Core.EntityMapConfigs.books
{
    // * EntityMapConfigs����MongoDb��ӳ�䣬�����Զ���һ���ֶε����л���ʽ���߱����ȣ�����Book�����ж�����ߣ�������ʵ�������ַ������������������ݿ��д洢ΪJSON���Ա�EF

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

using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.Mocking.Core.Entities;

public partial class MockWechatProfile
{
    public class MockWechatProfileBsonConfig : BsonConfig<MockWechatProfile>
    {
        protected override void Map(BsonClassMap<MockWechatProfile> map, BsonIndexConfig<MockWechatProfile> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(builder => builder.Ascending(x => x.OpenId), options =>
            {
                options.Background = true;
                options.Unique = true;
            });
        }
    }

    public class MockWechatProfileGqlConfig : GqlConfig.Object<MockWechatProfile>
    {
        protected override void Configure(IObjectTypeDescriptor<MockWechatProfile> descriptor)
        {
            descriptor.BindFieldsImplicitly();
        }
    }
}

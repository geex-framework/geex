using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.Mocking.Core.Entities;

public partial class MockWechatAuthorization
{
    public class MockWechatAuthorizationBsonConfig : BsonConfig<MockWechatAuthorization>
    {
        protected override void Map(BsonClassMap<MockWechatAuthorization> map, BsonIndexConfig<MockWechatAuthorization> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(builder => builder.Ascending(x => x.Token), options =>
            {
                options.Background = true;
                options.Unique = true;
            });
            indexConfig.MapIndex(builder => builder.Ascending(x => x.Code), options =>
            {
                options.Background = true;
                options.Sparse = true;
            });
        }
    }

    public class MockWechatAuthorizationGqlConfig : GqlConfig.Object<MockWechatAuthorization>
    {
        protected override void Configure(IObjectTypeDescriptor<MockWechatAuthorization> descriptor)
        {
            descriptor.BindFieldsImplicitly();
        }
    }
}

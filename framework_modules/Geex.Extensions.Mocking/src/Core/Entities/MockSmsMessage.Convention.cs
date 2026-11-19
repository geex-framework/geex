using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.Mocking.Core.Entities;

public partial class MockSmsMessage
{
    public class MockSmsMessageBsonConfig : BsonConfig<MockSmsMessage>
    {
        protected override void Map(BsonClassMap<MockSmsMessage> map, BsonIndexConfig<MockSmsMessage> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Hashed(y => y.PhoneNumber), options => options.Background = true);
            indexConfig.MapIndex(x => x.Descending(y => y.SentAt), options => options.Background = true);
        }
    }

    public class MockSmsMessageGqlConfig : GqlConfig.Object<MockSmsMessage>
    {
        protected override void Configure(IObjectTypeDescriptor<MockSmsMessage> descriptor)
        {
            descriptor.BindFieldsImplicitly();
        }
    }
}

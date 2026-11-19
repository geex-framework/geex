using Geex.Extensions.Payments;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.Mocking.Core.Entities;

public partial class MockPaymentTransaction
{
    public class MockPaymentTransactionBsonConfig : BsonConfig<MockPaymentTransaction>
    {
        protected override void Map(BsonClassMap<MockPaymentTransaction> map, BsonIndexConfig<MockPaymentTransaction> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(builder => builder.Ascending(x => x.Token), options =>
            {
                options.Background = true;
                options.Unique = true;
            });
            indexConfig.MapIndex(builder => builder.Ascending(x => x.ClientSn), options =>
            {
                options.Background = true;
                options.Unique = true;
            });
        }
    }

    public class MockPaymentTransactionGqlConfig : GqlConfig.Object<MockPaymentTransaction>
    {
        protected override void Configure(IObjectTypeDescriptor<MockPaymentTransaction> descriptor)
        {
            descriptor.BindFieldsImplicitly();
        }
    }
}

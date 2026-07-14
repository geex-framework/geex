using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.Mocking.Core.Entities;

public partial class MockPaymentRefund
{
    public class MockPaymentRefundBsonConfig : BsonConfig<MockPaymentRefund>
    {
        protected override void Map(BsonClassMap<MockPaymentRefund> map, BsonIndexConfig<MockPaymentRefund> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(builder => builder.Ascending(x => x.RefundRequestNo), options =>
            {
                options.Background = true;
                options.Unique = true;
            });
        }
    }

    public class MockPaymentRefundGqlConfig : GqlConfig.Object<MockPaymentRefund>
    {
        protected override void Configure(IObjectTypeDescriptor<MockPaymentRefund> descriptor)
        {
            descriptor.BindFieldsImplicitly();
        }
    }
}

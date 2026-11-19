using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.Mocking.Core.Entities;

public partial class MockRule
{
    public class MockRuleBsonConfig : BsonConfig<MockRule>
    {
        protected override void Map(BsonClassMap<MockRule> map, BsonIndexConfig<MockRule> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Hashed(y => y.Target), options => options.Background = true);
            indexConfig.MapIndex(x => x.Hashed(y => y.Operation), options => options.Background = true);
            indexConfig.MapIndex(x => x.Ascending(y => y.Priority), options => options.Background = true);
        }
    }

    public class MockRuleGqlConfig : GqlConfig.Object<MockRule>
    {
        protected override void Configure(IObjectTypeDescriptor<MockRule> descriptor)
        {
            descriptor.BindFieldsImplicitly();
        }
    }
}

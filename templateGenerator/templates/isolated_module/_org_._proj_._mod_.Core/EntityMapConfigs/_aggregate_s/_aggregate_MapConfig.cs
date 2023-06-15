using _org_._proj_._mod_.Core.Aggregates._aggregate_s;
using Geex.Common.Abstraction;
using MongoDB.Bson.Serialization;

namespace _org_._proj_._mod_.Core.EntityMapConfigs._aggregate_s
{
    public class _aggregate_MapConfig : IEntityMapConfig<_aggregate_>
    {
        public override void Map(BsonClassMap<_aggregate_> map)
        {
            map.AutoMap();
        }
    }
}

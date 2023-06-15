using x_Org_x.x_Proj_x.x_Mod_x.Core.Aggregates.x_Aggregate_xs;
using Geex.Common.Abstraction;
using MongoDB.Bson.Serialization;

namespace x_Org_x.x_Proj_x.x_Mod_x.Core.EntityMapConfigs.x_Aggregate_xs
{
    public class x_Aggregate_xMapConfig : EntityMapConfig<x_Aggregate_x>
    {
        public override void Map(BsonClassMap<x_Aggregate_x> map)
        {
            map.SetIsRootClass(true);
            map.Inherit<x_Aggregate_x>();
            map.AutoMap();
        }
    }
}

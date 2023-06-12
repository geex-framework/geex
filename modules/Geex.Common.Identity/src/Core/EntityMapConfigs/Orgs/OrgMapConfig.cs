using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Bson;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Identity.Core.Aggregates.Orgs;

using MongoDB.Bson.Serialization;

namespace Geex.Common.Identity.Core.EntityMapConfigs.Orgs
{
    public class OrgMapConfig : EntityMapConfig<Org>
    {
        public override void Map(BsonClassMap<Org> map)
        {
            map.Inherit<IOrg>();
            map.AutoMap();
        }
    }
}

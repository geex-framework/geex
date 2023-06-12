using Geex.Common.Abstraction;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.MultiTenant.Core.Aggregates.Tenants;

using MongoDB.Bson.Serialization;

namespace Geex.Common.MultiTenant.Core.EntityMapConfigs.Tenants
{
    public class TenantMapConfig : EntityMapConfig<Tenant>
    {
        public override void Map(BsonClassMap<Tenant> map)
        {
            map.Inherit<ITenant>();
            map.AutoMap();
        }
    }
}

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.MultiTenant.Core.Aggregates.Tenants;

using HotChocolate.Types;

using MongoDB.Bson.Serialization;

namespace Geex.Common.MultiTenant.Core.EntityMapConfigs.Tenants
{
    public class TenantEntityConfig : EntityConfig<Tenant>
    {
        protected override void Map(BsonClassMap<Tenant> map)
        {
            map.Inherit<ITenant>();
            map.AutoMap();
        }

        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<Tenant> descriptor)
        {
            descriptor.BindFieldsImplicitly();
            descriptor.Implements<InterfaceType<ITenant>>();
            descriptor.AuthorizeFieldsImplicitly();
            descriptor.ConfigEntity();
        }
    }
}

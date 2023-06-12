using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.MultiTenant.Api.Aggregates.Tenants;
using Geex.Common.MultiTenant.Core.Aggregates.Tenants;
using HotChocolate.Types;

namespace Geex.Common.MultiTenant.Gql.Schemas.Types
{
    public class TenantGqlType : ObjectType<Tenant>
    {
        protected override void Configure(IObjectTypeDescriptor<Tenant> descriptor)
        {
            // Implicitly binding all fields, if you want to bind fields explicitly, read more about hot chocolate
            descriptor.BindFieldsImplicitly();
            descriptor.Implements<InterfaceType<ITenant>>();
            descriptor.ConfigEntity();
            descriptor.AuthorizeFieldsImplicitly();
            base.Configure(descriptor);
        }
    }
}

using System;
using Geex.Extensions.Authorization;

namespace Geex.Extensions.MultiTenant.Api
{
    public class MultiTenantPermission : AppPermission<MultiTenantPermission>
    {
        /// <inheritdoc />
        public MultiTenantPermission(string value) : base($"{typeof(MultiTenantPermission).DomainName()}_{value}")
        {
        }
        public class TenantPermission : MultiTenantPermission
        {
            public static TenantPermission Query { get; } = new("query_tenants");
            public static TenantPermission Create { get; } = new("mutation_createTenant");
            public static TenantPermission Edit { get; } = new("mutation_editTenant");
            public static TenantPermission Delete { get; } = new("mutation_deleteTenant");

            /// <inheritdoc />
            public TenantPermission(string value) : base(value)
            {
            }
        }
    }
}

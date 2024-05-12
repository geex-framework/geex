using System;
using Geex.Common.Authorization;

namespace Geex.Common.MultiTenant.Api
{
    public class MultiTenantPermission : AppPermission<MultiTenantPermission>
    {
        /// <inheritdoc />
        public MultiTenantPermission(string value) : base($"{typeof(MultiTenantPermission).DomainName()}.{value}")
        {
        }
        public class TenantPermission : MultiTenantPermission
        {
            public static TenantPermission Query { get; } = new("query.tenants");
            public static TenantPermission Create { get; } = new("mutation.createTenant");
            public static TenantPermission Edit { get; } = new("mutation.editTenant");
            public static TenantPermission Delete { get; } = new("mutation.deleteTenant");

            /// <inheritdoc />
            public TenantPermission(string value) : base(value)
            {
            }
        }
    }
}

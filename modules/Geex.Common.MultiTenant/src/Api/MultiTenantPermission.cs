using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Authorization;

using HotChocolate.AspNetCore.Authorization;

using Humanizer;

namespace Geex.Common.MultiTenant.Api
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

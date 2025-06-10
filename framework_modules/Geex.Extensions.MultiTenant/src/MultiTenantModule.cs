using System.Security.Claims;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Extensions.Identity;
using Geex.Extensions.MultiTenant.Core;

using Microsoft.Extensions.DependencyInjection;

using Volo.Abp.Modularity;

namespace Geex.Extensions.MultiTenant
{
    [DependsOn(typeof(IdentityModule))]
    public class MultiTenantModule : GeexModule<MultiTenantModule>
    {
        /// <inheritdoc />
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddMultiTenant();
            base.ConfigureServices(context);
        }
    }
}

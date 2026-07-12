using Geex.Extensions.Identity;
using Geex.Extensions.MultiTenant.Core;
using Geex.Extensions.MultiTenant.Core.Providers;
using Geex.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            context.Services.TryAddTransient<IExternalTenantSyncProvider, NoOpExternalTenantSyncProvider>();
            base.ConfigureServices(context);
        }
    }
}

using Geex.Common.Abstractions;
using Geex.Common.MultiTenant.Core;

using Volo.Abp.Modularity;

namespace Geex.Common.MultiTenant
{
    [DependsOn(typeof(GeexCoreModule))]
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

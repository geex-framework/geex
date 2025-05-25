using Geex.Abstractions;
using Geex.Common.Accounting.Handlers;

using Microsoft.Extensions.DependencyInjection;

using Volo.Abp.Modularity;

namespace Geex.Common.Accounting
{
    [DependsOn(
        typeof(GeexCoreModule)
    )]
    public class AccountingModule : GeexModule<AccountingModule, AccountingModuleOptions>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<AccountHandler>();
            base.ConfigureServices(context);
        }
    }
}

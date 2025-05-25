using Geex.Abstractions;
using Geex.Common.Settings.Core.Handlers;
using Microsoft.Extensions.DependencyInjection;

using Volo.Abp.Modularity;

namespace Geex.Common.Settings
{
    [DependsOn(
        typeof(GeexCoreModule)
    )]
    public class SettingsModule : GeexModule<SettingsModule, SettingsModuleOptions>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // Register SettingHandler as both concrete implementation and service interface
            context.Services.AddTransient<SettingHandler>();
            context.Services.AddTransient<ISettingService>(sp => sp.GetRequiredService<SettingHandler>());

            base.ConfigureServices(context);
        }
    }
}

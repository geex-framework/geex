using Geex.Common.Abstractions;
using Geex.Common.Settings.Core;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Geex.Common.Settings
{
    [DependsOn(
        typeof(GeexCoreModule)
    )]
    public class SettingsModule : GeexModule<SettingsModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<SettingHandler>();
            base.ConfigureServices(context);
        }
    }
}

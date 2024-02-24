using System.Threading.Tasks;
using Geex.Common.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Common.BackgroundJob
{
    [DependsOn(typeof(GeexCoreModule))]
    public class BackgroundJobModule : GeexModule<BackgroundJobModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.TryAddSingleton<FireAndForgetTaskScheduler>(sp => new FireAndForgetTaskScheduler(sp));
            base.ConfigureServices(context);
        }

        /// <inheritdoc />
        public override void PostConfigureServices(ServiceConfigurationContext context)
        {
            //var moduleOptions = Configuration.GetModuleOptions<BackgroundJobModuleOptions>();
            base.PostConfigureServices(context);
        }

        /// <inheritdoc />
        public override Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            return base.OnPreApplicationInitializationAsync(context);
        }

        /// <inheritdoc />
        public override Task OnPostApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var moduleOptions = Configuration.GetModuleOptions<BackgroundJobModuleOptions>();
            return base.OnPostApplicationInitializationAsync(context);
        }
    }
}

using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Extensions.BackgroundJob.Gql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Extensions.BackgroundJob
{
    [DependsOn(typeof(GeexCoreModule))]
    public class BackgroundJobModule : GeexModule<BackgroundJobModule, BackgroundJobModuleOptions>
    {        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.TryAddSingleton<FireAndForgetTaskScheduler>(sp => new FireAndForgetTaskScheduler(sp));
            
            // GraphQL extensions will be automatically discovered through TryAddGeexAssembly
            // since they implement IScopedDependency through QueryExtension/MutationExtension
            base.ConfigureServices(context);
        }

        /// <inheritdoc />
        public override void PostConfigureServices(ServiceConfigurationContext context)
        {
            //var moduleOptions = Configuration.GetModuleOptions<BackgroundJobModuleOptions>();
            base.PostConfigureServices(context);
        }

        /// <inheritdoc />
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            base.OnPreApplicationInitialization(context);
        }

        /// <inheritdoc />
        public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
        {
            var moduleOptions = Configuration.GetModuleOptions<BackgroundJobModuleOptions>();
            base.OnPostApplicationInitialization(context);
        }
    }
}

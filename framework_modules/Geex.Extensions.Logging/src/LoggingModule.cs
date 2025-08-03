using Elastic.Apm.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Extensions.Logging
{
    [DependsOn(typeof(GeexCoreModule))]
    public class LoggingModule : GeexModule<LoggingModule, LoggingModuleOptions>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            SchemaBuilder
                .AddGeexTracing();
            base.ConfigureServices(context);
        }

        /// <inheritdoc />
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            if (this.ModuleOptions?.ElasticApm?.Enabled == true)
            {
                app.UseElasticApm(this.ModuleOptions.ConfigurationSection.GetSection(nameof(LoggingModuleOptions.ElasticApm)));
            }
            base.OnPreApplicationInitialization(context);
        }
    }
}

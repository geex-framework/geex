using System.Threading.Tasks;

using Elastic.Apm.AspNetCore;

using Geex.Common.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Common.Logging
{
    [DependsOn(typeof(GeexCoreModule))]
    public class LoggingModule : GeexModule<LoggingModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            SchemaBuilder
                .AddGeexTracing();
            base.ConfigureServices(context);
        }

        /// <inheritdoc />
        public override Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            var moduleOptions = Configuration.GetModuleOptions<LoggingModuleOptions>();
            if (moduleOptions.ElasticApm.Enabled)
            {
                app.UseElasticApm(moduleOptions.ConfigurationSection.GetSection(nameof(LoggingModuleOptions.ElasticApm)));
            }
            return base.OnPreApplicationInitializationAsync(context);
        }
    }
}

using Autofac.Extensions.DependencyInjection;

using GeexBox.ElasticSearch.Zero.Logging.Elasticsearch;
using Geex.Bms.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using Volo.Abp.DependencyInjection;


namespace Geex.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureLogging((ctx, builder) =>
                {
                    builder.AddGeexConsole();
                    builder.AddRollingFile();
                    if (ctx.Configuration.GetValue<bool>("Logging:Elasticsearch:Enabled"))
                    {
                        builder.AddElasticsearch();
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices((_, services) =>
                    {
                        services.AddApplication<AppModule>();
                    });
                    webBuilder.Configure(async (webHostBuilderContext, app) => { await app.InitializeApplicationAsync(); });
                });
        }
    }
}

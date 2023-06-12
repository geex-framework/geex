using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using GeexBox.ElasticSearch.Zero.Logging.Elasticsearch;
using Autofac.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace Geex.Bms.Demo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var serviceFactory = new AutofacServiceProviderFactory();
            return Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(serviceFactory)
                .ConfigureLogging((ctx, builder) =>
                {
                    if (ctx.Configuration.GetSection("Logging:Elasticsearch").GetChildren().Any())
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

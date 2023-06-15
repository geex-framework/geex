using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _org_._proj_._mod_.Api;
using Autofac.Extensions.DependencyInjection;
using Geex.Common.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using GeexBox.ElasticSearch.Zero.Logging.Elasticsearch;

namespace _org_._proj_._mod_
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var serviceFactory = new GeexServiceProviderFactory();
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
                        services.AddSingleton(serviceFactory.Builder);
                        services.AddApplication<AppModule>();
                    });
                    webBuilder.Configure((webHostBuilderContext, app) => { app.InitializeApplication(); });
                });
        }
    }
}

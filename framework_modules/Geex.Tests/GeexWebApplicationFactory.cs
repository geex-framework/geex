using Autofac.Extensions.DependencyInjection;

using Elastic.Apm.Api;

using Geex.Common;
using Geex.Tests.TestEntities;
using GeexBox.ElasticSearch.Zero.Logging.Elasticsearch;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;

using Volo.Abp.DependencyInjection;


namespace Geex.Tests
{
    public class GeexWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override IHostBuilder CreateHostBuilder()
        {
            return Program.CreateHostBuilder();
        }

        /// <inheritdoc />
        public override async ValueTask DisposeAsync()
        {
            var uow = this.Services.GetService<IUnitOfWork>();
            await uow.DeleteAsync<TestEntity>();
            await uow.CommitAsync();
            await base.DisposeAsync();
        }
    }
    public class Program
    {
        public static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureLogging((ctx, builder) =>
                {
                    builder.AddGeexConsole();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseEnvironment("Test");
                    webBuilder.ConfigureServices((_, services) =>
                    {
                        services.AddApplication<TestModule>();
                    });
                    webBuilder.Configure(async (webHostBuilderContext, app) => { await app.InitializeApplicationAsync(); });
                });
        }
    }
}

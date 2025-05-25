using Autofac.Extensions.DependencyInjection;

using Geex.Abstractions;
using Geex.Tests.TestEntities;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            await uow.SaveChanges();
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

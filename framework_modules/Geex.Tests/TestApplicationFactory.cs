using Autofac.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Volo.Abp.DependencyInjection;

namespace Geex.Tests;

public class TestApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await IntegrationTestDataCleaner.CleanAsync();
    }

    /// <inheritdoc />
    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
    }

    /// <inheritdoc />
    protected override IHostBuilder? CreateHostBuilder()
    {
        return Program.CreateHostBuilder();
    }

    /// <inheritdoc />
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = builder.Build();
        host.Start();
        return host;
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

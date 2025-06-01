using Autofac.Extensions.DependencyInjection;
using Geex.Extensions.Authentication.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp.DependencyInjection;

namespace Geex.Tests;

public class TestApplicationFactory : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureClient(HttpClient client)
    {
        _ = this.Services.GetService<SuperAdminAuthHandler>();
        var token = SuperAdminAuthHandler.AdminToken;
        client.DefaultRequestHeaders.Add("Authorization", $"SuperAdmin {token}");
        base.ConfigureClient(client);
    }

    public IServiceScope StartTestScope(out IServiceProvider service)
    {
        var scope = this.Services.CreateScope();
        service = scope.ServiceProvider;
        return scope;
    }

    protected override IHostBuilder CreateHostBuilder()
    {
        return Program.CreateHostBuilder();
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
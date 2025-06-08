using Autofac.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MongoDB.Entities;

using Volo.Abp.DependencyInjection;

namespace Geex.Tests;

public class TestApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHostBuilder CreateHostBuilder()
    {
        return Program.CreateHostBuilder();
    }

    /// <inheritdoc />
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = builder.Build();
        host.ConfigServiceLocator().Start();
        return host;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        var listCollectionNames = DB.DefaultDb.ListCollectionNames();
        while (listCollectionNames.MoveNext())
        {
            foreach (var collectionName in listCollectionNames.Current)
            {
                DB.DefaultDb.DropCollection(collectionName);
            }
        }
        base.Dispose(disposing);
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
using Geex.Common;
using Geex.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Entities;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Geex.Tests;

[DependsOn(typeof(GeexCoreModule))]
public class TestModule : GeexEntryModule<TestModule>
{
    /// <inheritdoc />
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        Console.WriteLine(Env.EnvironmentName);
        base.OnApplicationInitialization(context);
    }
}
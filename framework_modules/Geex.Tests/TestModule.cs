using Geex.Common;
using Geex.Common.Abstractions;
using Geex.Common.AuditLogs;
using Geex.Common.BlobStorage.Core;

using HotChocolate;
using HotChocolate.Execution;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Volo.Abp;
using Volo.Abp.Modularity;

namespace Geex.Tests;

[DependsOn(typeof(GeexCoreModule),
    typeof(BlobStorageCoreModule),
    typeof(AuditLogsModule))]
public class TestModule : GeexEntryModule<TestModule>
{
    /// <inheritdoc />
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        Console.WriteLine(Env.EnvironmentName);
        base.OnApplicationInitialization(context);
    }

    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var builder = this.SchemaBuilder;
        context.Services.AddSingleton<ISchema>((sp) => builder.BuildSchemaAsync().Result);
        base.ConfigureServices(context);
    }
}
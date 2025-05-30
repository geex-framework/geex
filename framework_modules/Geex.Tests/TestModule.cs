using Geex.Common;
using Geex.Abstractions;
using Geex.Extensions.ApprovalFlows;
using Geex.Extensions.AuditLogs;
using Geex.Extensions.Settings;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Entities;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Geex.Tests;

[DependsOn(typeof(GeexCoreModule),
    typeof(GeexCommonModule),
    typeof(ApprovalFlowModule))]
public class TestModule : GeexEntryModule<TestModule>
{
    /// <inheritdoc />
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        Console.WriteLine(Env.EnvironmentName);
        foreach (var collectionName in DB.DefaultDb.ListCollectionNames().Current)
        {
            DB.DefaultDb.DropCollection(collectionName);
        }
        base.OnApplicationInitialization(context);
    }

    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var builder = this.SchemaBuilder;
        context.Services.AddSingleton<IRequestExecutorBuilder>((sp) => builder);
        context.Services.AddSingleton<ISchema>((sp) => builder.BuildSchemaAsync().GetAwaiter().GetResult());
        base.ConfigureServices(context);
    }
}

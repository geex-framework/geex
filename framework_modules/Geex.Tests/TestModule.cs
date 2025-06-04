using Geex.Common;
using Geex.Abstractions;
using Geex.Extensions.ApprovalFlows;
using Geex.Extensions.AuditLogs;
using Geex.Extensions.MultiTenant;
using Geex.Extensions.Settings;
using Geex.Tests.FeatureTests;
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
    typeof(MultiTenantModule),
    typeof(ApprovalFlowModule))]
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
        context.Services.AddSingleton<IRequestExecutorBuilder>((sp) => builder);
        context.Services.AddSingleton<ISchema>((sp) => builder.BuildSchemaAsync().GetAwaiter().GetResult());
        context.Services.AddJob<TestStatefulCronJob>("* * * * * *");
        base.ConfigureServices(context);
    }
}

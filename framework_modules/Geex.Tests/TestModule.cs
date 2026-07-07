using Geex.Common;
using Geex.Extensions.ApprovalFlows;
using Geex.Extensions.Messaging;
using Geex.Extensions.MultiTenant;
using Geex.Extensions.Payments;
using Geex.Tests.FeatureTests;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Geex.Tests;

[DependsOn(typeof(GeexCoreModule),
    typeof(GeexCommonModule),
    typeof(MultiTenantModule),
    typeof(ApprovalFlowModule),
    typeof(PaymentsModule))]
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

        var paymentsOptions = context.Services.GetSingletonInstance<PaymentsModuleOptions>();
        paymentsOptions.UseVirtualTransaction = true;
        paymentsOptions.VirtualTransactionSimulateCallbacks = false;
        paymentsOptions.PaymentExpireMinutes = 0;

        var messagingOptions = context.Services.GetSingletonInstance<MessagingModuleOptions>();
        messagingOptions.UseVirtualSms = true;
    }
}

using Geex.Common;
using Geex.Extensions.ApprovalFlows;
using Geex.Extensions.Authentication;
using Geex.Extensions.Authentication.Wechat;
using Geex.Extensions.Authentication.Wechat.Core;
using Geex.Extensions.Captcha;
using Geex.Extensions.MultiTenant;
using Geex.Extensions.Payments;
using Geex.Tests.FeatureTests;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Geex.Tests;

[DependsOn(typeof(GeexCoreModule),
    typeof(GeexCommonModule),
    typeof(MultiTenantModule),
    typeof(ApprovalFlowModule),
    typeof(PaymentsModule),
    typeof(CaptchaModule),
    typeof(AuthenticationWechatModule))]
public class TestModule : GeexEntryModule<TestModule>
{
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        Console.WriteLine(Env.EnvironmentName);
        base.OnApplicationInitialization(context);
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var builder = this.SchemaBuilder;
        context.Services.AddSingleton<IRequestExecutorBuilder>((sp) => builder);
        context.Services.AddSingleton<ISchema>((sp) => builder.BuildSchemaAsync().GetAwaiter().GetResult());
        context.Services.AddJob<TestStatefulCronJob>("* * * * * *");
        context.Services.AddScoped<ILoginProvider, TestLoginProvider>();
        _ = TestLoginProviders.TestExternal;

        context.Services.RemoveAll<IWechatApiClient>();
        context.Services.AddSingleton<IWechatApiClient, FakeWechatApiClient>();

        base.ConfigureServices(context);
    }
}

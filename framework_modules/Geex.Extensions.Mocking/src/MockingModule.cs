using Geex.Extensions.Authentication;
using Geex.Extensions.Authentication.Wechat.Core;
using Geex.Extensions.Messaging;
using Geex.Extensions.Mocking.Core.Decorators;
using Geex.Extensions.Payments;
using Geex.MultiTenant;
using Geex.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace Geex.Extensions.Mocking;

[DependsOn(typeof(GeexCoreModule), typeof(AuthenticationModule))]
public class MockingModule : GeexModule<MockingModule, MockingModuleOptions>
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        if (!ModuleOptions.Enabled)
        {
            return;
        }

        context.Services.AddHttpContextAccessor();
        context.Services.TryAddTransient(typeof(LazyService<>));

        context.Services.DecorateRegistered<IWechatApiClient>((sp, inner) =>
            new MockWechatApiClient(inner, sp.GetRequiredService<IUnitOfWork>()));

        context.Services.DecorateRegistered<ISmsSender>((sp, inner) =>
            new MockSmsSender(inner, sp.GetRequiredService<IUnitOfWork>(), sp.GetService<LazyService<ICurrentTenant>>()));

        context.Services.DecorateRegistered<IPaymentProvider>((sp, inner) =>
            new MockPaymentProvider(inner, sp.GetRequiredService<IUnitOfWork>(), sp.GetRequiredService<IHttpContextAccessor>(), sp.GetService<LazyService<ICurrentTenant>>()));

        context.Services.DecorateRegistered<IExternalTenantSyncProvider>((sp, inner) =>
            new MockExternalTenantSyncProvider(inner, sp.GetRequiredService<IUnitOfWork>()));

        base.ConfigureServices(context);
    }
}

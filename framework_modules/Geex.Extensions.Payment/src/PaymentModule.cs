using Geex.Extensions.MultiTenant;
using Geex.Extensions.Payment.Core.Handlers;
using Geex.Extensions.Payment.Core.Providers;
using Geex.Extensions.Payment.Extensions;
using Geex.Extensions.Payment.Infrastructure;
using Geex.Extensions.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Extensions.Payment;

[DependsOn(typeof(GeexCoreModule), typeof(MultiTenantModule), typeof(SettingsModule))]
public class PaymentModule : GeexModule<PaymentModule, PaymentModuleOptions>
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<PaymentHandler>();
        context.Services.AddTransient<IPaymentService>(sp => sp.GetRequiredService<PaymentHandler>());
        context.Services.AddTransient<PaymentNotifyUrlResolver>();

        if (ModuleOptions.UseMockProviders)
        {
            context.Services.AddTransient<IPaymentProvider, MockPaymentProvider>();
        }
        else
        {
            context.Services.AddTransient<IPaymentProvider, WeChatPayProvider>();
            context.Services.AddTransient<IPaymentProvider, AlipayProvider>();
        }

        base.ConfigureServices(context);
    }

    public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        app.UseEndpoints(endpoints => endpoints.UsePaymentNotify());
        base.OnPostApplicationInitialization(context);
    }
}

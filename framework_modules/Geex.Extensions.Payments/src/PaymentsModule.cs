using Geex.Extensions.BackgroundJob;
using Geex.Extensions.MultiTenant;
using Geex.Extensions.Payments.Core.Handlers;
using Geex.Extensions.Payments.Core.Jobs;
using Geex.Extensions.Payments.Core.Providers;
using Geex.Extensions.Payments.Core.Providers.Shouqianba;
using Geex.Extensions.Payments.Extensions;
using Geex.Extensions.Payments.Infrastructure;
using Geex.Extensions.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Extensions.Payments;

[DependsOn(typeof(GeexCoreModule), typeof(MultiTenantModule), typeof(SettingsModule), typeof(BackgroundJobModule))]
public class PaymentsModule : GeexModule<PaymentsModule, PaymentsModuleOptions>
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClient<ShouqianbaApiClient>();
        context.Services.AddTransient<PaymentHandler>();
        context.Services.AddTransient<PaymentNotifyUrlResolver>();
        context.Services.AddTransient<ShouqianbaCredentialsProvider>();

        if (ModuleOptions.UseVirtualTransaction)
        {
            context.Services.AddTransient<IPaymentProvider, VirtualTransactionPaymentProvider>();
        }
        else
        {
            context.Services.AddTransient<IPaymentProvider, ShouqianbaPaymentProvider>();
        }

        context.Services.AddJob<PaymentTimeoutJob>();
        base.ConfigureServices(context);
    }

    public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        app.UseEndpoints(endpoints => endpoints.UsePaymentsNotify());
        base.OnPostApplicationInitialization(context);
    }
}

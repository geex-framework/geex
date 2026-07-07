using Geex.Extensions.Authentication;
using Geex.Extensions.Messaging.Core.Handlers;
using Geex.Extensions.Messaging.Core.Sms;
using Geex.Extensions.Settings;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Extensions.Messaging
{
    [DependsOn(typeof(GeexCoreModule), typeof(SettingsModule), typeof(AuthenticationModule))]
    public class MessagingModule : GeexModule<MessagingModule, MessagingModuleOptions>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<TencentCloudSmsCredentialsProvider>();
            context.Services.AddTransient<SmsHandler>();
            context.Services.AddTransient<ISmsSender>(sp =>
            {
                var options = sp.GetRequiredService<MessagingModuleOptions>();
                return options.UseVirtualSms
                    ? new VirtualSmsSender()
                    : new TencentCloudSmsSender(sp.GetRequiredService<TencentCloudSmsCredentialsProvider>());
            });

            base.ConfigureServices(context);
        }
    }
}

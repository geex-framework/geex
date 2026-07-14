using Geex.Extensions.Authentication.Wechat.Core;
using Geex.Extensions.Identity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Extensions.Authentication.Wechat
{
    [DependsOn(typeof(AuthenticationModule), typeof(IdentityModule))]
    public class AuthenticationWechatModule : GeexModule<AuthenticationWechatModule, AuthenticationWechatModuleOptions>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddHttpClient<IWechatApiClient, WechatApiClient>(client =>
            {
                client.BaseAddress = new Uri("https://api.weixin.qq.com/");
            });

            context.Services.AddScoped<ILoginProvider, WechatWebLoginProvider>();
            context.Services.AddScoped<ILoginProvider, WechatMiniProgramLoginProvider>();

            _ = WechatLoginProviders.WechatWeb;
            _ = WechatLoginProviders.WechatMiniProgram;

            base.ConfigureServices(context);
        }
    }
}

using Geex.Extensions.Captcha.Core.Handlers;
using Geex.Extensions.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Extensions.Captcha;

[DependsOn(typeof(MessagingModule))]
public class CaptchaModule : GeexModule<CaptchaModule>
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<CaptchaHandler>();
        base.ConfigureServices(context);
    }
}

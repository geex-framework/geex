using Geex.Extensions.Messaging;
using Volo.Abp.Modularity;

namespace Geex.Common.Captcha
{
    [DependsOn(typeof(MessagingModule))]
    public class CaptchaModule : GeexModule<CaptchaModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
        }
    }
}

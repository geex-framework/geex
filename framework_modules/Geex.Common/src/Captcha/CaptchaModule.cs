using Geex.Common.Abstractions;
using Volo.Abp.Modularity;

namespace Geex.Common.Captcha
{
    [DependsOn(
    )]
    public class CaptchaModule : GeexModule<CaptchaModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
        }
    }
}

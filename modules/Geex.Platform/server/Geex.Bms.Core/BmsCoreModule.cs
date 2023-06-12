using System.Threading.Tasks;
using Geex.Common;
using Geex.Common.Abstractions;
using Geex.Common.Authentication;
using Geex.Common.Identity.Core;
using Volo.Abp.Modularity;
using Geex.Bms.Core.CacheData;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Geex.Common.MultiTenant;
using Geex.Bms.Core.Authentication.LoginProviders;
using Geex.Common.Captcha;
using Geex.Bms.Core.Localization;
using Geex.Bms.Demo.Core;

namespace Geex.Bms.Core
{
    [DependsOn(
        typeof(GeexCoreModule),
        typeof(AuthenticationModule),
        typeof(MultiTenantModule),
        typeof(GeexCommonModule),
        typeof(CaptchaModule),
        typeof(CacheDataModule),
        typeof(LocalizationModule),
        typeof(IdentityCoreModule),
        typeof(BmsDemoCoreModule)
        )]
    public class BmsCoreModule : GeexModule<BmsCoreModule>
    {
        /// <inheritdoc />
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddScoped<IExternalLoginProvider, GeexLoginProvider>();
            //context.Services.AddJob<SyncExchangeRateJob>("0 0 6,20 * * ?");
            base.ConfigureServices(context);
        }

        /// <inheritdoc />
        public override Task OnPostApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            return base.OnPostApplicationInitializationAsync(context);
        }
    }
}

using System.Threading.Tasks;
using Geex.Common;
using Geex.Common.Abstractions;
using Geex.Common.Authentication;
using Geex.Common.Identity.Core;
using Volo.Abp.Modularity;
using x_Org_x.x_Proj_x.Core.CacheData;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Geex.Common.MultiTenant;
using x_Org_x.x_Proj_x.Core.Authentication.LoginProviders;
using Geex.Common.Captcha;
using x_Org_x.x_Proj_x.Core.Localization;
using x_Org_x.x_Proj_x.x_Mod_x.Core;

namespace x_Org_x.x_Proj_x.Core
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
        typeof(x_Proj_xx_Mod_xCoreModule)
        )]
    public class x_Proj_xCoreModule : GeexModule<x_Proj_xCoreModule>
    {
        /// <inheritdoc />
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddScoped<IExternalLoginProvider, x_Org_xLoginProvider>();
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

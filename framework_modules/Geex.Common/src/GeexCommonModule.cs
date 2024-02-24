using Geex.Common.Abstractions;
using Geex.Common.Accounting;
using Geex.Common.Authorization;
using Geex.Common.BackgroundJob;
using Geex.Common.BlobStorage.Core;
using Geex.Common.Identity.Core;
using Geex.Common.Logging;
using Geex.Common.Messaging.Core;
using Geex.Common.Settings;
using Volo.Abp.Modularity;

namespace Geex.Common
{
    [DependsOn(
        typeof(GeexCoreModule),
        typeof(AccountingModule),
        typeof(IdentityCoreModule),
        typeof(LoggingModule),
        typeof(MessagingCoreModule),
        typeof(BlobStorageCoreModule),
        typeof(BackgroundJobModule),
        typeof(SettingsModule),
        typeof(AuthorizationModule)
        )]
    public class GeexCommonModule : GeexModule<GeexCommonModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
        }
    }
}

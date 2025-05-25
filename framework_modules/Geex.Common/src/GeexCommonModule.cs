using Geex.Abstractions;
using Geex.Common.Accounting;
using Geex.Common.AuditLogs;
using Geex.Common.Authentication;
using Geex.Common.Authorization;
using Geex.Common.BackgroundJob;
using Geex.Common.BlobStorage;
using Geex.Common.Identity.Core;
using Geex.Common.Logging;
using Geex.Common.Messaging.Core;
using Geex.Common.Settings;
using Volo.Abp.Modularity;

namespace Geex.Common
{
    [DependsOn(
        typeof(GeexCoreModule),
        typeof(AuthenticationModule),
        typeof(AuthorizationModule),
        typeof(AccountingModule),
        typeof(IdentityCoreModule),
        typeof(LoggingModule),
        typeof(MessagingCoreModule),
        typeof(BlobStorageModule),
        typeof(BackgroundJobModule),
        typeof(SettingsModule),
        typeof(AuditLogsModule)
        )]
    public class GeexCommonModule : GeexModule<GeexCommonModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
        }
    }
}

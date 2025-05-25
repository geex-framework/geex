using Geex.Abstractions;
using Geex.Common.AuditLogs;
using Geex.Common.Authentication;
using Geex.Common.Authorization;
using Geex.Common.BackgroundJob;
using Geex.Common.BlobStorage;
using Geex.Common.Identity;
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
        typeof(IdentityModule),
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

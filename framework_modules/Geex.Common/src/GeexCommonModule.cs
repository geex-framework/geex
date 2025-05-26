using Geex.Abstractions;
using Geex.Extensions.AuditLogs;
using Geex.Extensions.Authentication;
using Geex.Extensions.Authorization;
using Geex.Extensions.BackgroundJob;
using Geex.Extensions.BlobStorage;
using Geex.Extensions.Identity;
using Geex.Extensions.Logging;
using Geex.Extensions.Messaging.Core;
using Geex.Extensions.Settings;
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

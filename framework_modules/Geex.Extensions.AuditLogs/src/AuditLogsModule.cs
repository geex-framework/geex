using Geex.Extensions.AuditLogs.Core.Handlers;
using Geex.Extensions.AuditLogs.Core.Jobs;
using Geex.Extensions.AuditLogs.Utils;
using Geex.Extensions.Authentication;
using Geex.Extensions.BackgroundJob;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Extensions.AuditLogs
{
    [DependsOn(
        typeof(AuthenticationModule),
        typeof(BackgroundJobModule)
    )]
    public partial class AuditLogsModule : GeexModule<AuditLogsModule, AuditLogsModuleOptions>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            SchemaBuilder.AddDirectiveType(typeof(AuditDirectiveType.Config));
            SchemaBuilder.TryAddTypeInterceptor<AuditLogsTypeInterceptor>();
            context.Services.AddTransient<AuditLogHandler>();
            context.Services.AddJob<AuditLogRetentionJob>("0 0 3 * * *");
            base.ConfigureServices(context);
        }
    }
}

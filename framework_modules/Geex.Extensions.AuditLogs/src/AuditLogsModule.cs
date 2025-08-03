using Geex.Extensions.AuditLogs.Utils;
using Geex.Extensions.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Extensions.AuditLogs
{
    [DependsOn(
        typeof(AuthenticationModule)
    )]
    public partial class AuditLogsModule : GeexModule<AuditLogsModule, AuditLogsModuleOptions>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            SchemaBuilder.AddDirectiveType(typeof(AuditDirectiveType.Config));
            SchemaBuilder.TryAddTypeInterceptor<AuditLogsTypeInterceptor>();
            // todo: 增加接口定义及删查功能
            base.ConfigureServices(context);
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            base.OnPreApplicationInitialization(context);
        }
    }
}

using System.Threading.Tasks;
using Geex.Common.Abstractions;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Common.ApprovalFlows
{
    [DependsOn(
        typeof(GeexCoreModule)
    )]
    public class ApprovalFlowModule : GeexModule<ApprovalFlowModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
        }

        public override Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            return base.OnPreApplicationInitializationAsync(context);
        }
    }
}

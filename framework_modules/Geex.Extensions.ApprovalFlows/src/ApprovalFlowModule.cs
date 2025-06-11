using System.Threading.Tasks;

using Geex.Abstractions;
using Microsoft.Extensions.DependencyInjection;

using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Extensions.ApprovalFlows;

[DependsOn(
    typeof(GeexCoreModule)
)]
public class ApprovalFlowModule : GeexModule<ApprovalFlowModule, ApprovalFlowModuleOptions>
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        SchemaBuilder.AddInterfaceType<IApproveEntity>(x =>
                {
                    x.BindFieldsExplicitly();
                    //x.Implements<IEntityType>();
                    x.Field(y => y.ApproveStatus);
                    x.Field(y => y.Submittable);
                })
                .AddEnumType<ApproveStatus>();
        SchemaBuilder.TryAddTypeInterceptor<ApproveEntityTypeInterceptor>();
        base.ConfigureServices(context);
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        base.OnPreApplicationInitialization(context);
    }
}
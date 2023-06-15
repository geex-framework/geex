using Geex.Common.Abstractions;
using Volo.Abp.Modularity;

namespace x_Org_x.x_Proj_x.Core
{
    [DependsOn(
        typeof(x_Proj_xCoreModule)
        )]
    public class AppModule : GeexEntryModule<AppModule>
    {

    }
}

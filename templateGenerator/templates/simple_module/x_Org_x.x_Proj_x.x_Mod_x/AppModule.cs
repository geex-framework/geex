using x_Org_x.x_Proj_x.x_Mod_x.Core;

using Geex.Common;
using Geex.Common.Abstractions;

using Volo.Abp.Modularity;

namespace x_Org_x.x_Proj_x.x_Mod_x {
    [DependsOn(
        typeof(GeexCommonModule),
        typeof(x_Proj_xx_Mod_xCoreModule)
        )]
    public class AppModule : GeexEntryModule<AppModule> {

    }
}

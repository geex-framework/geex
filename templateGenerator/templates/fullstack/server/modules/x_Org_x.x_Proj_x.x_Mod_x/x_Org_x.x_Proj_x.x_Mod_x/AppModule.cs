using x_Org_x.x_Proj_x.x_Mod_x.Core;

using Geex.Common;
using Geex.Common.Abstractions;

using Volo.Abp.Modularity;

namespace x_Org_x.x_Proj_x.x_Mod_x {
    [DependsOn(
        typeof(GeexCommonModule),
        typeof(_proj__mod_CoreModule)
        )]
    public class AppModule : GeexEntryModule<AppModule> {

    }
}

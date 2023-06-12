using Geex.Bms.Demo.Core;

using Geex.Common;
using Geex.Common.Abstractions;

using Volo.Abp.Modularity;

namespace Geex.Bms.Demo {
    [DependsOn(
        typeof(GeexCommonModule),
        typeof(bmsdemoCoreModule)
        )]
    public class AppModule : GeexEntryModule<AppModule> {

    }
}

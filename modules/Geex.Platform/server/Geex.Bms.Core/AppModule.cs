using Geex.Common.Abstractions;
using Volo.Abp.Modularity;

namespace Geex.Bms.Core
{
    [DependsOn(
        typeof(BmsCoreModule)
        )]
    public class AppModule : GeexEntryModule<AppModule>
    {

    }
}

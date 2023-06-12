using Geex.Common;
using Geex.Common.Abstractions;

using Volo.Abp.Modularity;

namespace Geex.Bms.Demo.Core
{
    [DependsOn(typeof(GeexCoreModule))]
    public class BmsDemoCoreModule : GeexModule<BmsDemoCoreModule>
    {
    }
}

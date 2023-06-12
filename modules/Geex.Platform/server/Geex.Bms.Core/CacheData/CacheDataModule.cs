using Geex.Common;
using Geex.Common.Abstractions;

using Volo.Abp.Modularity;

namespace Geex.Bms.Core.CacheData
{
    [DependsOn(
        typeof(GeexCoreModule))]
    public class CacheDataModule : GeexModule<CacheDataModule>
    {

    }
}

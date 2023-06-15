using Geex.Common.Abstractions;
using Volo.Abp.Modularity;
using _org_._proj_._mod_.Api;
namespace _org_._proj_._mod_.Core
{
    [DependsOn(typeof(_proj__mod_ApiModule))]
    public class _proj__mod_CoreModule : GeexModule<_proj__mod_CoreModule>
    {
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using _org_._proj_._mod_.Api;
using _org_._proj_._mod_.Core;

using Geex.Common;
using Geex.Common.Abstractions;
using Geex.Common.Settings;

using Volo.Abp.Modularity;

namespace _org_._proj_._mod_
{
    [DependsOn(
        typeof(GeexCommonModule),
        typeof(_proj__mod_CoreModule),
        typeof(_proj__mod_ApiModule)
        )]
    public class AppModule : GeexEntryModule<AppModule>
    {

    }
}

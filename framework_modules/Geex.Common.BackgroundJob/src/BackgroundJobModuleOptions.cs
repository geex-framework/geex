using System.Collections.Generic;
using Geex.Common.Abstractions;

namespace Geex.Common.BackgroundJob
{
    public class BackgroundJobModuleOptions : GeexModuleOption<BackgroundJobModule>
    {
        public Dictionary<string, string> JobConfigs { get; set; } = new Dictionary<string, string>();
        public bool Disabled { get; set; }
    }
}

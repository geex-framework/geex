using System.Collections.Generic;

namespace Geex.Extensions.BackgroundJob
{
    public class BackgroundJobModuleOptions : GeexModuleOptions<BackgroundJobModule>
    {
        public Dictionary<string, string> JobConfigs { get; set; } = new Dictionary<string, string>();
        public bool Disabled { get; set; }
    }
}

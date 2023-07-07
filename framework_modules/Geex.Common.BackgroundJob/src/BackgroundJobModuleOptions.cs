using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstractions;

using HotChocolate.Execution.Options;

namespace Geex.Common.BackgroundJob
{
    public class BackgroundJobModuleOptions : GeexModuleOption<BackgroundJobModule>
    {
        public Dictionary<string, string> JobConfigs { get; set; } = new Dictionary<string, string>();
        public bool Disabled { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geex.Common.Settings.Core
{
    public static class Extension
    {
        public static string GetRedisKey(this Setting setting)
        {
            return $"{nameof(Setting)}:{setting.Scope}{(setting.ScopedKey == default ? "" : $":{setting.ScopedKey}")}:{setting.Name}";
        }
    }
}

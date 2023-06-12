using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Configuration
{
    public static class Extensions
    {
        public static string GetAppHostAddress(this IConfiguration configuration)
        {
            return configuration.GetValue<string>("App:HostAddress");
        }

        public static TModuleOption GetModuleOptions<TModuleOption>(this IConfiguration configuration)
        {
            return configuration.GetSection(typeof(TModuleOption).Name).Get<TModuleOption>();
        }
    }
}

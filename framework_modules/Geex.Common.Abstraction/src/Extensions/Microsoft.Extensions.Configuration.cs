using Geex.Common.Abstractions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Configuration
{
    public static class Extensions
    {
        public static string GetAppHostAddress(this IConfiguration configuration)
        {
            return configuration.GetValue<string>("App:HostAddress");
        }

        public static TModuleOption GetModuleOptions<TModuleOption>(this IConfiguration configuration) where TModuleOption : GeexModuleOption
        {
            var configurationSection = configuration.GetSection(typeof(TModuleOption).Name);
            var moduleOptions = configurationSection.Get<TModuleOption>();
            moduleOptions.ConfigurationSection = configurationSection;
            return moduleOptions;
        }
    }
}

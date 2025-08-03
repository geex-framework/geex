using System.Text.Json.Nodes;
using Geex.Extensions.Settings;

namespace Geex.Tests
{
    public class TestModuleSettings : SettingDefinition
    {
        /// <inheritdoc />
        public TestModuleSettings(string name, SettingScopeEnumeration[] validScopes = default, string? description = null, bool isHiddenForClients = false, JsonNode defaultValue = null) : base(name, validScopes, description, isHiddenForClients, defaultValue)
        {
        }

        public static TestModuleSettings GlobalSetting { get; } = new(nameof(GlobalSetting), new[] { SettingScopeEnumeration.Global, }, "Global");
        public static TestModuleSettings TenantSetting { get; } = new(nameof(TenantSetting), new[] { SettingScopeEnumeration.Tenant, }, "Tenant");
        public static TestModuleSettings UserSetting { get; } = new(nameof(UserSetting), new[] { SettingScopeEnumeration.User, }, "User");

    }
}

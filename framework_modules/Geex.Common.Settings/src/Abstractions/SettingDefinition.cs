using System.Text.Json.Nodes;
using Geex.Abstractions;

namespace Geex.Common.Settings
{
    public class SettingDefinition : Enumeration<SettingDefinition>
    {
        public string Description { get; }
        public SettingScopeEnumeration[] ValidScopes { get; }
        public bool IsHiddenForClients { get; }
        public JsonNode DefaultValue { get; }

        public SettingDefinition(string name, SettingScopeEnumeration[] validScopes = default, string? description = null, bool isHiddenForClients = false, JsonNode defaultValue = null) : base(name, name)
        {
            Description = description ?? name;
            ValidScopes = validScopes ?? new[] { SettingScopeEnumeration.Global, SettingScopeEnumeration.User, };
            IsHiddenForClients = isHiddenForClients;
            DefaultValue = defaultValue;
        }
    }
}

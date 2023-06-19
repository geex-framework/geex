using System;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;

using Geex.Common.Abstractions;
using Geex.Common.Settings.Core;

using JetBrains.Annotations;

namespace Geex.Common.Settings.Abstraction
{
    public class SettingDefinition : Enumeration<SettingDefinition>
    {
        public string Description { get; }
        public SettingScopeEnumeration[] ValidScopes { get; }
        public bool IsHiddenForClients { get; }
        public JsonNode DefaultValue { get; }
        public Setting DefaultInstance => new (this, DefaultValue, SettingScopeEnumeration.Global);

        public SettingDefinition(string name, SettingScopeEnumeration[] validScopes = default, string? description = null, bool isHiddenForClients = false, JsonNode defaultValue = null) : base(name, name)
        {
            Description = description ?? name;
            ValidScopes = validScopes ?? new[] { SettingScopeEnumeration.Global, SettingScopeEnumeration.User, };
            IsHiddenForClients = isHiddenForClients;
            DefaultValue = defaultValue;
        }
    }
}
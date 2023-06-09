﻿using System.Text.Json.Nodes;
using Geex.Common.Settings.Abstraction;
using JetBrains.Annotations;

namespace x_Org_x.x_Proj_x.Core
{
    public class AppSettings : SettingDefinition
    {
        public static AppSettings AppName { get; } = new(nameof(AppName), new[] { SettingScopeEnumeration.Global }, defaultValue: JsonValue.Create("x_proj_x"));

        public static AppSettings AppMenu { get; } = new(nameof(AppMenu), new[] { SettingScopeEnumeration.Global });
        public static AppSettings Permissions { get; } = new(nameof(Permissions), new[] { SettingScopeEnumeration.Global });

        public AppSettings([NotNull] string name,
            SettingScopeEnumeration[] validScopes = default,
            [CanBeNull] string? description = null, bool isHiddenForClients = false, JsonNode defaultValue = null) : base("App" + name,
            validScopes, description, isHiddenForClients, defaultValue)
        {
        }
    }
}

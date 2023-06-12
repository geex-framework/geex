using System.Text.Json.Nodes;
using Geex.Common.Settings.Abstraction;
using JetBrains.Annotations;

namespace Geex.Bms.Core.Localization
{
    public class LocalizationSettings : SettingDefinition
    {
        public LocalizationSettings([NotNull] string name, SettingScopeEnumeration[] validScopes,
            [CanBeNull] string? description = null, bool isHiddenForClients = false, JsonNode? defaultValue = null) : base(nameof(Localization) + name, validScopes, description, isHiddenForClients, defaultValue)
        {
        }
        public static LocalizationSettings Language { get; } = new(nameof(Language), new[] { SettingScopeEnumeration.Global, SettingScopeEnumeration.User }, defaultValue: JsonValue.Create("zh-cn"));
        public static LocalizationSettings Data { get; } = new(nameof(Data), new[] { SettingScopeEnumeration.Global }, defaultValue: JsonNode.Parse(
            """
            {
                "zh-cn":{},
                "en-us":{}
            }
            """
            ));
    }
}

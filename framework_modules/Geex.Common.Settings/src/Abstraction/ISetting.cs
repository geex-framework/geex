using System.Text.Json.Nodes;
using Geex.Common.Abstraction.Settings;
using MongoDB.Entities;

namespace Geex.Common.Settings.Abstraction
{
    public interface ISetting : IEntityBase
    {
        SettingScopeEnumeration Scope { get; }
        string? ScopedKey { get; }
        JsonNode? Value { get; }
        SettingDefinition Name { get; }
    }
}

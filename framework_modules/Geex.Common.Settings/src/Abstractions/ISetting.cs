using System.Text.Json.Nodes;
using MongoDB.Entities;

namespace Geex.Common.Settings
{
    public interface ISetting : IEntityBase
    {
        SettingScopeEnumeration Scope { get; }
        string? ScopedKey { get; }
        JsonNode? Value { get; }
        SettingDefinition Name { get; }
    }
}

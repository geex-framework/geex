using System.Text.Json.Nodes;
using MongoDB.Entities;

namespace Geex.Extensions.Settings
{
    public interface ISetting : IEntityBase
    {
        SettingScopeEnumeration Scope { get; }
        string? ScopedKey { get; }
        JsonNode? Value { get; }
        SettingDefinition Name { get; }
    }
}

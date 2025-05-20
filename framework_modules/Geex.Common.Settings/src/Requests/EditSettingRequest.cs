using System.Text.Json.Nodes;
using Geex.Common.Abstraction.Settings;
using Geex.Common.Settings.Abstraction;
using MediatR;

namespace Geex.Common.Requests.Settings
{
    public record EditSettingRequest : IRequest<ISetting>
    {
        public SettingDefinition? Name { get; set; }
        public JsonNode? Value { get; set; }
        public string? ScopedKey { get; set; }
        public SettingScopeEnumeration? Scope { get; set; }
    }
}
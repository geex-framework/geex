using System.Text.Json.Nodes;
using Geex.Common.Settings.Abstraction;
using Geex.Common.Settings.Api.Aggregates.Settings;
using MediatR;

namespace Geex.Common.Requests.Settings
{
    public class EditSettingRequest : IRequest<ISetting>
    {
        public SettingDefinition? Name { get; set; }
        public JsonNode? Value { get; set; }
        public string? ScopedKey { get; set; }
        public SettingScopeEnumeration? Scope { get; set; }
    }
}
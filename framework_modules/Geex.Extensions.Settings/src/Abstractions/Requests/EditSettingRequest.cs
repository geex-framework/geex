﻿using System.Text.Json.Nodes;
using MediatX;

namespace Geex.Extensions.Settings.Requests
{
    public record EditSettingRequest : IRequest<ISetting>
    {
        public SettingDefinition? Name { get; set; }
        public JsonNode? Value { get; set; }
        public string? ScopedKey { get; set; }
        public SettingScopeEnumeration? Scope { get; set; }
    }
}

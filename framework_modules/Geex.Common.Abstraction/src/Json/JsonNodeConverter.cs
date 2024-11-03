﻿using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Geex.Common.Abstraction.Json
{
    public class JsonNodeConverter : JsonConverter<JsonNode>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsAssignableTo<JsonNode>();
        }
        /// <inheritdoc />
        public override JsonNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonNode.Parse(ref reader);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, JsonNode value, JsonSerializerOptions options)
        {
            value.WriteTo(writer);
        }
    }
}

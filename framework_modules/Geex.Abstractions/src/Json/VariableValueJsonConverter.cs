using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace Geex.Json
{
    public class ValueNodeJsonConverter : JsonConverter<IValueNode>
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsAssignableTo<IValueNode>();
        }

        public override IValueNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, IValueNode value, JsonSerializerOptions options)
        {
            switch (value.Value)
            {
                case List<ObjectFieldNode> fieldList:
                    writer.WriteStartObject();
                    foreach (var fieldNode in fieldList)
                    {
                        writer.WritePropertyName(fieldNode.Name.Value);
                        JsonSerializer.Serialize(writer, fieldNode.Value, options);
                    }
                    writer.WriteEndObject();
                    break;
                case List<IValueNode> valueNodes:
                    writer.WriteStartArray();
                    foreach (var valueNode in valueNodes)
                    {
                        JsonSerializer.Serialize(writer, valueNode, options);
                    }
                    writer.WriteEndArray();
                    break;
                case ListValueNode nestedFieldList:
                    writer.WriteStartArray();
                    foreach (var fieldList in nestedFieldList.Items)
                    {
                        JsonSerializer.Serialize(writer, fieldList, options);
                    }
                    writer.WriteEndArray();
                    break;
                case ObjectFieldNode { Value: FileValueNode fileValue }:
                    writer.WriteStringValue($"<file:{fileValue.Value.Name}, size:{fileValue.Value.Length}>");
                    break;
                case IValueNode valueNode:
                    JsonSerializer.Serialize(writer, valueNode.Value, options);
                    break;
                default:
                    if (value.TryGetValueKind(out var valueKind))
                    {
                        switch (valueKind)
                        {
                            case ValueKind.String:
                                JsonSerializer.Serialize(writer, value.Value, options);
                                break;
                            case ValueKind.Integer:
                                JsonSerializer.Serialize(writer, int.Parse(value.Value.ToString()), options);
                                break;
                            case ValueKind.Float:
                                JsonSerializer.Serialize(writer, decimal.Parse(value.Value.ToString()), options);
                                break;
                            case ValueKind.Boolean:
                                JsonSerializer.Serialize(writer, bool.Parse(value.Value.ToString()), options);
                                break;
                            default:
                                JsonSerializer.Serialize(writer, value.Value, options);
                                break;
                        }
                    }
                    break;
            }
        }
    }

    public class VariableValueCollectionJsonConverter : JsonConverter<IVariableValueCollection>
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsAssignableTo<IVariableValueCollection>();
        }

        public override IVariableValueCollection Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, IVariableValueCollection value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var variable in value)
            {
                writer.WritePropertyName(variable.Name);
                JsonSerializer.Serialize(writer, variable.Value, options);
            }

            writer.WriteEndObject();
        }
    }
}

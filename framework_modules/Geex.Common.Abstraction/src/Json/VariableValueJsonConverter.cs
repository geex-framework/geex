using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace Geex.Common.Abstraction.Json
{
    public class ValueNodeJsonConverter : JsonConverter<IValueNode>
    {
        public override IValueNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, IValueNode value, JsonSerializerOptions options)
        {
            var variableValue = value.Value;
            switch (variableValue)
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
                    JsonSerializer.Serialize(writer, variableValue, options);
                    break;
            }
        }
    }

    public class VariableValueCollectionJsonConverter : JsonConverter<IVariableValueCollection>
    {
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

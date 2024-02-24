using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Entities.Utilities
{
    public class JsonNodeSerializer : SerializerBase<JsonNode>
    {
        /// <inheritdoc />
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonNode value)
        {
            var writer = context.Writer;
            if (value is JsonObject jsonObject)
            {
                var bson = BsonDocument.Parse(jsonObject.ToJsonString());
                writer.WriteRawBsonDocument(new BsonDocumentReader(bson).ReadRawBsonDocument());
                return;
            }
            if (value is JsonArray jsonArray)
            {
                writer.WriteStartArray();
                foreach (var jsonNode in jsonArray)
                {
                    this.Serialize(context, jsonNode);
                }
                //var bson = BsonDocument.Parse(jsonArray.ToJsonString());
                writer.WriteEndArray();
                return;
                //writer.WriteRawBsonDocument(new BsonDocumentReader(bson).ReadRawBsonDocument());
            }

            if (value is null)
            {
                writer.WriteNull();
            }

            if (value is JsonValue _value)
            {
                if (_value.TryGetValue(out JsonElement element))
                {
                    switch (element.ValueKind)
                    {
                        case JsonValueKind.Undefined:
                        case JsonValueKind.Object:
                        case JsonValueKind.Array:
                        case JsonValueKind.Null:
                            BsonSerializer.LookupSerializer<object>().Serialize(context, _value.GetValue<object>());
                            break;
                        case JsonValueKind.String:
                            BsonSerializer.LookupSerializer<string>().Serialize(context, element.GetString());
                            break;
                        case JsonValueKind.Number:
                            if (element.TryGetDecimal(out var @decimal))
                            {
                                BsonSerializer.LookupSerializer<decimal>().Serialize(context, @decimal);
                            }
                            else
                            {
                                BsonSerializer.LookupSerializer<int>().Serialize(context, element.GetInt32());
                            }
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            BsonSerializer.LookupSerializer<bool>().Serialize(context, element.GetBoolean());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    return;
                }
                BsonSerializer.LookupSerializer<object>().Serialize(context, _value.GetValue<object>());
                return;
            }
        }

        /// <inheritdoc />
        public override JsonNode Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            if (reader.CurrentBsonType == BsonType.Document)
            {
                var bson = new BsonDocument();
                new BsonDocumentWriter(bson).WriteRawBsonDocument(reader.ReadRawBsonDocument());
                var json = JsonNode.Parse(bson.ToJson());
                return json;
            }
            else if (reader.CurrentBsonType == BsonType.Array)
            {
                var bson = new BsonDocument();
                var writer = new BsonDocumentWriter(bson);
                writer.WriteStartDocument();
                writer.WriteName("_result_");
                writer.WriteRawBsonArray(reader.ReadRawBsonArray());
                writer.WriteEndDocument();
                var json = JsonNode.Parse(bson["_result_"].ToJson());
                return json;
            }
            else if (reader.CurrentBsonType == BsonType.Null)
            {
                reader.ReadNull();
                return default(JsonNode);
            }
            else
            {
                JsonNode result = null;
                switch (reader.CurrentBsonType)
                {
                    case BsonType.Double:
                        result = reader.ReadDouble();
                        break;
                    case BsonType.String:
                        result = reader.ReadString();
                        break;
                    case BsonType.Binary:
                        result = JsonValue.Create(reader.ReadBinaryData().Bytes);
                        break;
                    case BsonType.Null:
                    case BsonType.Undefined:
                        result = JsonValue.Create((object)null);
                        break;
                    case BsonType.ObjectId:
                        result = JsonValue.Create(reader.ReadObjectId());
                        break;
                    case BsonType.Boolean:
                        result = JsonValue.Create(reader.ReadBoolean());
                        break;
                    case BsonType.DateTime:
                        result = JsonValue.Create(reader.ReadDateTime());
                        break;
                    case BsonType.Int32:
                        result = JsonValue.Create(reader.ReadInt32());
                        break;
                    case BsonType.Int64:
                        result = JsonValue.Create(reader.ReadInt64());
                        break;
                    case BsonType.Decimal128:
                        result = JsonValue.Create(reader.ReadDecimal128());
                        break;
                    default:
                        result = JsonNode.Parse(BsonSerializer.LookupSerializer<object>().Deserialize(context).ToJson());
                        break;
                }
                return result;
            }
            throw new NotSupportedException($"JsonNode must be type of array or document. current type is: [{reader.CurrentBsonType}]");
        }
    }

    public class ObjectIdCompatibleStringSerializer : StringSerializer
    {

        /// <inheritdoc />
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
        {
            var writer = context.Writer;
            if (value == default)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteString(value);
        }

        /// <inheritdoc />
        public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            if (reader.CurrentBsonType == BsonType.ObjectId)
            {
                var objectId = reader.ReadObjectId();
                return objectId.ToString();
            }
            return base.Deserialize(context, args);
        }
    }
    public class StringCompatibleObjectIdSerializer : ObjectIdSerializer
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ObjectId value)
        {
            var writer = context.Writer;
            if (value == default)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteObjectId(value);
        }

        /// <inheritdoc />
        public override ObjectId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            if (reader.CurrentBsonType == BsonType.String)
            {
                var objectId = reader.ReadString();
                return ObjectId.Parse(objectId);
            }
            return base.Deserialize(context, args);
        }
    }
}
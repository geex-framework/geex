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
            if (value is JsonValue jsonValue)
            {
                BsonSerializer.LookupSerializer<JsonValue>().Serialize(context, args, value);
                return;
            }
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
                else
                {
                    BsonSerializer.LookupSerializer<object>().Serialize(context, _value.GetValue<object>());
                }
                return;
            }
        }

        /// <inheritdoc />
        public override JsonNode Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            try
            {
                if (reader.CurrentBsonType == BsonType.Document)
                {
                    var bson = new BsonDocument();
                    new BsonDocumentWriter(bson).WriteRawBsonDocument(reader.ReadRawBsonDocument());
                    var json = JsonNode.Parse(bson.ToJson());
                    return json;
                }
                else if (reader.CurrentBsonType == BsonType.Array)
                {
                    var jsonArray = new JsonArray();
                    reader.ReadStartArray();

                    while (reader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        switch (reader.CurrentBsonType)
                        {
                            case BsonType.Document:
                                var bsonDoc = new BsonDocument();
                                new BsonDocumentWriter(bsonDoc).WriteRawBsonDocument(reader.ReadRawBsonDocument());
                                jsonArray.Add(JsonNode.Parse(bsonDoc.ToJson()));
                                break;
                            case BsonType.Array:
                                // Handle nested arrays by recursively calling Deserialize
                                var nestedContext = BsonDeserializationContext.CreateRoot(reader);
                                jsonArray.Add(Deserialize(nestedContext, args));
                                break;
                            case BsonType.String:
                                jsonArray.Add(JsonValue.Create(reader.ReadString()));
                                break;
                            case BsonType.Int32:
                                jsonArray.Add(JsonValue.Create(reader.ReadInt32()));
                                break;
                            case BsonType.Int64:
                                jsonArray.Add(JsonValue.Create(reader.ReadInt64()));
                                break;
                            case BsonType.Double:
                                jsonArray.Add(JsonValue.Create(reader.ReadDouble()));
                                break;
                            case BsonType.Boolean:
                                jsonArray.Add(JsonValue.Create(reader.ReadBoolean()));
                                break;
                            case BsonType.ObjectId:
                                jsonArray.Add(JsonValue.Create(reader.ReadObjectId().ToString()));
                                break;
                            case BsonType.DateTime:
                                jsonArray.Add(JsonValue.Create(
                                    DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadDateTime())));
                                break;
                            case BsonType.Decimal128:
                                jsonArray.Add(JsonValue.Create((decimal)reader.ReadDecimal128()));
                                break;
                            case BsonType.Null:
                                reader.ReadNull();
                                jsonArray.Add(null);
                                break;
                            case BsonType.Binary:
                                jsonArray.Add(JsonValue.Create(reader.ReadBinaryData().Bytes));
                                break;
                            default:
                                throw new NotSupportedException($"Unsupported BSON type in array: {reader.CurrentBsonType}");
                        }
                    }

                    reader.ReadEndArray();
                    return jsonArray;
                }
                else if (reader.CurrentBsonType == BsonType.Null)
                {
                    reader.ReadNull();
                    return default(JsonNode);
                }
                else
                {
                    var result = BsonSerializer.LookupSerializer<JsonValue>().Deserialize(context, args);
                    return result;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new NotSupportedException($"JsonNode must be type of array or document. current type is: [{reader.CurrentBsonType}]", e);
            }
        }
    }
    public class JsonValueSerializer : SerializerBase<JsonValue>
    {
        /// <inheritdoc />
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonValue value)
        {
            var writer = context.Writer;

            if (value is null)
            {
                writer.WriteNull();
            }

            switch (value.GetValueKind())
            {
                case JsonValueKind.Undefined:
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                case JsonValueKind.Null:
                    BsonSerializer.LookupSerializer<object>().Serialize(context, value.GetValue<object>());
                    break;
                case JsonValueKind.String:
                    BsonSerializer.LookupSerializer<string>().Serialize(context, value.GetValue<string>());
                    break;
                case JsonValueKind.Number:
                    if (value.TryGetValue<decimal>(out var decimalValue))
                    {
                        BsonSerializer.LookupSerializer<decimal>().Serialize(context, decimalValue);
                    }
                    else if (value.TryGetValue<int>(out var intValue))
                    {
                        BsonSerializer.LookupSerializer<int>().Serialize(context, intValue);
                    }
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    BsonSerializer.LookupSerializer<bool>().Serialize(context, value.GetValue<bool>());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return;
        }

        /// <inheritdoc />
        public override JsonValue Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            if (reader.CurrentBsonType is BsonType.Document or BsonType.Array)
            {
                throw new NotSupportedException($"JsonValue must be type of array or document. current type is: [{reader.CurrentBsonType}]");
            }
            else if (reader.CurrentBsonType == BsonType.Null)
            {
                reader.ReadNull();
                return default(JsonValue);
            }
            else
            {
                JsonValue result = reader.CurrentBsonType switch
                {
                    BsonType.Double => JsonValue.Create(reader.ReadDouble()),
                    BsonType.String => JsonValue.Create(reader.ReadString()),
                    BsonType.Binary => JsonValue.Create(reader.ReadBinaryData().Bytes),
                    BsonType.Null or BsonType.Undefined => JsonValue.Create((object)null),
                    BsonType.ObjectId => JsonValue.Create(reader.ReadObjectId().ToString()),
                    BsonType.Boolean => JsonValue.Create(reader.ReadBoolean()),
                    BsonType.DateTime => JsonValue.Create(
                        DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadDateTime())),
                    BsonType.Int32 => JsonValue.Create(reader.ReadInt32()),
                    BsonType.Int64 => JsonValue.Create(reader.ReadInt64()),
                    BsonType.Decimal128 => JsonValue.Create((decimal)reader.ReadDecimal128()),
                    _ => throw new NotSupportedException($"JsonValue must be type of array or document. current type is: [{reader.CurrentBsonType}]")
                };
                return result;
            }
        }
    }
}

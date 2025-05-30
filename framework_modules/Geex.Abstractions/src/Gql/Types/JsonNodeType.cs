﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;
using MongoDB.Bson;

namespace Geex.Gql.Types;

public class JsonNodeType : ScalarType<JsonNode>
{
    /// <inheritdoc />
    public static MethodInfo _ParseValue = typeof(AnyType).GetMethod(nameof(ParseValue), BindingFlags.NonPublic);

    private ObjectValueToJsonNodeConverter _objectValueToDictConverter = new ObjectValueToJsonNodeConverter();

    /// <inheritdoc />
    public JsonNodeType() : base("Any")
    {
    }

    public override bool IsInstanceOfType(IValueNode literal)
    {
        switch (literal)
        {
            case null:
                throw new ArgumentNullException(nameof(literal));
            case ObjectValueNode _:
            case ListValueNode _:
            case NullValueNode _:
            case StringValueNode _:
                return true;
            default:
                return true;
        }
    }

    /// <inheritdoc />
    public override object? ParseLiteral(IValueNode literal)
    {
        //return literal.Value;
        var literalValue = literal.Value;
        if (literalValue is JsonNode jsonNode)
        {
            return jsonNode;
        }

        if (literal.TryGetValueKind(out var kind))
        {
            return kind switch
            {
                ValueKind.String => JsonValue.Create(literalValue as string),
                ValueKind.Integer => JsonValue.Create(int.Parse(literalValue.ToString())),
                ValueKind.Float => JsonValue.Create(decimal.Parse(literalValue.ToString())),
                ValueKind.Boolean => JsonValue.Create(bool.Parse(literalValue.ToString())),
                ValueKind.Enum => JsonValue.Create(literalValue.ToString()),
                ValueKind.Object => JsonNode.Parse(literalValue.As<IList<ObjectFieldNode>>()
                    .ToDictionary(x => x.Name.Value, x => this.ParseLiteral(x.Value))
                    .ToJson()),
                ValueKind.List => JsonNode.Parse(literalValue.ToJson()),
                ValueKind.Null => JsonValue.Create(default(object)),
                ValueKind.Unknown => JsonValue.Create(literalValue.ToString()),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        return JsonNode.Parse(literal.ToJson());
    }

    public override IValueNode ParseValue(object? value)
    {
        switch (value)
        {
            case null:
                return (IValueNode)NullValueNode.Default;
            case string:
            case short:
            case int:
            case long:
            case float:
            case double:
            case Decimal:
            case bool:
            case sbyte:
            case byte:
                throw new SerializationException("Scalar_Cannot_ParseValue", (IType)this);
            default:
                return _ParseValue.Invoke(this, new[] { value, (ISet<object>)new HashSet<object>() }) as IValueNode;
        }
    }

    public override IValueNode ParseResult(object? resultValue) => this.ParseValue(resultValue);

    /// <inheritdoc />
    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        runtimeValue = resultValue;
        return true;
    }

    private object JsonNodeToObject(JsonNode? jsonNode)
    {
        if (jsonNode == null) return null;
        switch (jsonNode)
        {
            case JsonObject jsonObject:
                return new Dictionary<string, object>(jsonObject.Select(x => new KeyValuePair<string, object>(x.Key, this.JsonNodeToObject(x.Value))));
            case JsonArray jsonArray:
                return jsonArray.Select(x => JsonNodeToObject(x)).ToList();
            case JsonValue jsonValue:
                if (jsonValue.TryGetValue(out JsonElement value))
                {
                    switch (value.ValueKind)
                    {
                        case JsonValueKind.Undefined:
                        case JsonValueKind.Object:
                        case JsonValueKind.Null:
                        case JsonValueKind.Array:
                            return value.GetValue();
                        case JsonValueKind.String:
                            return value.GetString();
                        case JsonValueKind.Number:
                            return value.GetDecimal();
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            return value.GetBoolean();
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (jsonValue.TryGetValue(out Decimal128 decimalValue))
                {
                    return (decimal)decimalValue;
                }
                return jsonValue.GetValue<object>();


            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <inheritdoc />
    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        resultValue = JsonNodeToObject(runtimeValue as JsonNode);
        return true;
    }
}

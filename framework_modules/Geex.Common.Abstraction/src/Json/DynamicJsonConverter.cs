using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using ImpromptuInterface;

namespace Geex.Common.Json
{
    /// <summary>
    /// Temp Dynamic Converter
    /// by:tchivs@live.cn
    /// </summary>
    public class DynamicJsonConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return (typeToConvert.IsInterface && !typeToConvert.IsAssignableTo<IEnumerable>()) || typeToConvert.IsDynamic() || typeToConvert == typeof(object);
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return Activator.CreateInstance(typeof(DynamicJsonConverterInstance<>).MakeGenericType(typeToConvert)) as JsonConverter;
        }
        #region read methods
        private static object ReadObject(JsonElement jsonElement)
        {
            IDictionary<string, object> expandoObject = new ExpandoObject();
            foreach (var obj in jsonElement.EnumerateObject())
            {
                var k = obj.Name;
                var value = ReadValue(obj.Value);
                expandoObject[k] = value;
            }
            return expandoObject;
        }

        private static object? ReadValue(JsonElement jsonElement)
        {
            object? result = null;
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Object:
                    result = ReadObject(jsonElement);
                    break;
                case JsonValueKind.Array:
                    result = ReadList(jsonElement);
                    break;
                case JsonValueKind.String:
                    //TODO: Missing Datetime&Bytes Convert
                    result = jsonElement.GetString();
                    break;
                case JsonValueKind.Number:
                    //TODO: more num type
                    result = 0;
                    if (jsonElement.TryGetInt64(out long l))
                    {
                        result = l;
                    }
                    break;
                case JsonValueKind.True:
                    result = true;
                    break;
                case JsonValueKind.False:
                    result = false;
                    break;
                case JsonValueKind.Undefined:
                case JsonValueKind.Null:
                    result = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return result;
        }
        private static object? ReadList(JsonElement jsonElement)
        {
            IList<object?> list = new List<object?>();
            foreach (var item in jsonElement.EnumerateArray())
            {
                list.Add(ReadValue(item));
            }
            return list.Count == 0 ? null : list;
        }
        #endregion

        internal class DynamicJsonConverterInstance<T> : JsonConverter<T> where T : class
        {

            public override T Read(ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options)
            {

                if (reader.TokenType == JsonTokenType.True)
                {
                    return (T)(object)true;
                }

                if (reader.TokenType == JsonTokenType.False)
                {
                    return (T)(object)false;
                }

                if (reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetInt64(out long l))
                    {
                        return (T)(object)l;
                    }

                    return (T)(object)reader.GetDouble();
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    if (reader.TryGetDateTime(out DateTime datetime))
                    {
                        return (T)(object)datetime;
                    }

                    return (T)(object)reader.GetString();
                }

                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    using JsonDocument documentV = JsonDocument.ParseValue(ref reader);
                    if (typeof(T).IsInterface)
                    {
                        return ReadObject(documentV.RootElement).ActLike<T>();
                    }
                    else
                    {
                        return (T)ReadObject(documentV.RootElement);
                    }
                }
                // Use JsonElement as fallback.
                // Newtonsoft uses JArray or JObject.
                JsonDocument document = JsonDocument.ParseValue(ref reader);
                return (T)(object)document.RootElement.Clone();
            }


            public override void Write(Utf8JsonWriter writer,
                T value,
                JsonSerializerOptions options)
            {
                if (value is IActLikeProxy proxy)
                {
                    var data = (proxy.Original as ExpandoObject).ToDictionary();
                    writer.WriteRaw(JsonSerializer.Serialize(data, options));
                }
                else if (value is ExpandoObject)
                {
                    var settingsCopy = new JsonSerializerOptions(options);
                    settingsCopy.Converters.RemoveAll(x => x is DynamicJsonConverter);
                    writer.WriteRaw(JsonSerializer.Serialize(value, settingsCopy));
                }
                //else if (value is IEnumerable enumerable)
                //{
                //    writer.WriteStartArray();
                //    var enumerator = enumerable.GetEnumerator();
                //    while (enumerator.MoveNext())
                //    {
                //        writer.WriteRaw(JsonSerializer.Serialize(enumerator.Current, enumerator.Current.GetType(), options));
                //    }
                //    writer.WriteEndArray();
                //}
                else
                {
                    writer.WriteRaw(JsonSerializer.Serialize(value, value.GetType(), options));
                }
            }
        }
    }
}

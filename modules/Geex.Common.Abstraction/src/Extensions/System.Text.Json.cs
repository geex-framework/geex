﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Unicode;

using Geex.Common.Abstraction.Json;
using Geex.Common.Abstractions;
using Geex.Common.Json;

// ReSharper disable once CheckNamespace
namespace System.Text.Json
{
    public static class Json
    {
        public static void WriteRaw(this Utf8JsonWriter writer, string jsonRaw)
        {
            using JsonDocument document = JsonDocument.Parse(jsonRaw);
            document.RootElement.WriteTo(writer);
        }
        static Json()
        {
            DefaultSerializeSettings.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            DefaultSerializeSettings.PropertyNameCaseInsensitive = true;
            DefaultSerializeSettings.Converters.Add(new JsonNodeConverter());
            DefaultSerializeSettings.Converters.Add(new JsonStringEnumConverter());
            DefaultSerializeSettings.Converters.Add(new EnumerationConverter());
            DefaultSerializeSettings.Converters.Add(new ExceptionConverter());
            DefaultSerializeSettings.Converters.Add(new GqlSyntaxNodeConverter());
            DefaultSerializeSettings.Converters.Add(new DynamicJsonConverter());
            DefaultSerializeSettings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        }
        public static JsonSerializerOptions DefaultSerializeSettings { get; set; } = new();

        public static string ToJsonSafe<T>(this T @this)
        {
            try
            {
                return @this.ToJson();
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static string ToJson<T>(this T @this)
        {
            return JsonSerializer.Serialize(@this, DefaultSerializeSettings);
        }

        public static T? ToObjectSafe<T>(this string @this)
        {
            try
            {
                return @this.ToObject<T>();
            }
            catch (Exception e)
            {
                return default;
            }
        }

        public static T? ToObject<T>(this string @this)
        {
            return JsonSerializer.Deserialize<T>(@this, DefaultSerializeSettings);
        }

        public static object? ToObject(this string @this, Type type)
        {
            return JsonSerializer.Deserialize(@this, type, DefaultSerializeSettings);
        }

        public static T? ToObject<T>(this string @this, T typeHint)
        {
            return JsonSerializer.Deserialize<T>(@this, DefaultSerializeSettings);
        }

        public static T? ToObjectSafe<T>(this JsonNode @this)
        {
            try
            {
                return @this.ToObject<T>();
            }
            catch (Exception e)
            {
                return default;
            }
        }

        public static T? ToObject<T>(this JsonNode @this)
        {
            return JsonSerializer.Deserialize<T>(@this, DefaultSerializeSettings);
        }

        public static object? ToObject(this JsonNode @this, Type type)
        {
            return JsonSerializer.Deserialize(@this, type, DefaultSerializeSettings);
        }

        public static T? ToObject<T>(this JsonNode @this, T typeHint)
        {
            return JsonSerializer.Deserialize<T>(@this, DefaultSerializeSettings);
        }

        public static T? ToObjectSafe<T>(this JsonElement @this)
        {
            try
            {
                return @this.ToObject<T>();
            }
            catch (Exception e)
            {
                return default;
            }
        }

        public static T? ToObject<T>(this JsonElement @this)
        {
            return JsonSerializer.Deserialize<T>(@this, DefaultSerializeSettings);
        }

        public static object? ToObject(this JsonElement @this, Type type)
        {
            return JsonSerializer.Deserialize(@this, type, DefaultSerializeSettings);
        }

        public static T? ToObject<T>(this JsonElement @this, T typeHint)
        {
            return JsonSerializer.Deserialize<T>(@this, DefaultSerializeSettings);
        }
        public static JsonNode? ToNode(this JsonElement @this)
        {
             return JsonSerializer.Deserialize<JsonNode>(@this, DefaultSerializeSettings);
        }
        public static JsonElement? ToNode(this JsonNode @this)
        {
             return JsonSerializer.Deserialize<JsonElement>(@this, DefaultSerializeSettings);
        }

        public static T? GetValue<T>(this JsonNode @this, string key)
        {
             var paths = key.Split(':', StringSplitOptions.RemoveEmptyEntries);
             var currentNode = @this;
             foreach (var path in paths)
             {
                 currentNode = currentNode[path];
             }
             return currentNode.Deserialize<T>();
        }

        public static object? GetValue(this JsonNode @this)
        {
             return @this.Deserialize<object>();
        }

        public static object? GetValue(this JsonElement @this)
        {
             return @this.Deserialize<object>();
        }

        public static T? GetValue<T>(this JsonElement @this, string key)
        {
             var paths = key.Split(':', StringSplitOptions.RemoveEmptyEntries);
             var currentNode = @this;
             foreach (var path in paths)
             {
                 currentNode = currentNode.GetProperty(path);
             }
             return currentNode.Deserialize<T>();
        }
    }

    public class EnumerationConverter<T> : JsonConverter<T> where T : class
    {
        private static Type classEnumRealType = typeof(T).GetClassEnumRealType();
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var data = reader.GetString();
            return typeof(Enumeration<>).MakeGenericType(classEnumRealType).GetMethod(nameof(Enumeration.FromValue), types: new[] { typeof(string) })?.Invoke(null, new[] { data }) as T;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var data = value.ToString();
            writer.WriteStringValue(data);
        }
    }
}

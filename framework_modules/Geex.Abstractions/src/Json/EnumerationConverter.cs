using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Geex.Json
{
    public class EnumerationConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsAssignableTo<IEnumeration>();
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(EnumerationConverter<>)
                    .MakeGenericType(new Type[] { typeToConvert }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            return converter;
        }
    }
}

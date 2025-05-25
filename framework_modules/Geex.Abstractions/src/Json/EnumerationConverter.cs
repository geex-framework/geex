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
            Type enumType = typeToConvert.GetClassEnumRealType();

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(EnumerationConverter<>)
                    .MakeGenericType(new Type[] { enumType }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            return converter;
        }
    }
}

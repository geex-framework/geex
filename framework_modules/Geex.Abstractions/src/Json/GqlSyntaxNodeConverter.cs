//using System;
//using System.Text.Json;
//using System.Text.Json.Serialization;
//using HotChocolate.Language;
//using HotChocolate.Types;

//namespace Geex.Abstractions.Json
//{
//    public class _GqlSyntaxNodeConverter<T> : JsonConverter<T> where T : ISyntaxNode
//    {
//        /// <summary>Reads and converts the JSON to type <typeparamref name="T" />.</summary>
//        /// <param name="reader">The reader.</param>
//        /// <param name="typeToConvert">The type to convert.</param>
//        /// <param name="options">An object that specifies serialization options to use.</param>
//        /// <returns>The converted value.</returns>
//        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//        {
//            throw new NotImplementedException();
//        }

//        /// <summary>Writes a specified value as JSON.</summary>
//        /// <param name="writer">The writer to write to.</param>
//        /// <param name="value">The value to convert to JSON.</param>
//        /// <param name="options">An object that specifies serialization options to use.</param>
//        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
//        {
//            var str = value is ObjectFieldNode { Value: FileValueNode fileValue } ? $"<file:{fileValue.Value.Name}>" : value.ToString();
//            writer.WriteStringValue(str);
//        }
//    }
//    public class GqlSyntaxNodeConverter : JsonConverterFactory
//    {
//        /// <summary>When overridden in a derived class, determines whether the converter instance can convert the specified object type.</summary>
//        /// <param name="typeToConvert">The type of the object to check whether it can be converted by this converter instance.</param>
//        /// <returns>
//        /// <see langword="true" /> if the instance can convert the specified object type; otherwise, <see langword="false" />.</returns>
//        public override bool CanConvert(Type typeToConvert)
//        {
//            return typeToConvert.IsAssignableTo<ISyntaxNode>();
//        }

//        /// <summary>Creates a converter for a specified type.</summary>
//        /// <param name="typeToConvert">The type handled by the converter.</param>
//        /// <param name="options">The serialization options to use.</param>
//        /// <returns>A converter for which <typeparamref name="T" /> is compatible with <paramref name="typeToConvert" />.</returns>
//        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
//        {
//            return Activator.CreateInstance(typeof(_GqlSyntaxNodeConverter<>).MakeGenericType(typeToConvert)) as JsonConverter;
//        }
//    }
//}

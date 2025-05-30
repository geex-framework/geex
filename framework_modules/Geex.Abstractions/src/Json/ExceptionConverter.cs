﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Geex.Json
{
    public class ExceptionConverter : JsonConverter<Exception>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsAssignableTo<Exception>();
        }
        public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
        {
            WriteException(writer, value);
            // Add any other propoerties that you may want to include in your JSON.
            // ...
        }

        private static void WriteException(Utf8JsonWriter writer, Exception exception, bool isNested = false)
        {
            if (isNested)
            {
                writer.WriteStartObject("innerException");
            }
            else
            {
                writer.WriteStartObject();
            }
            writer.WriteString("message", exception.Message);
            writer.WriteString("stackTrace", exception.StackTrace);
            writer.WriteString("source", exception.Source);
            if (exception.InnerException != default)
            {
                WriteException(writer, exception.InnerException, true);
            }
            writer.WriteEndObject();
        }
    }
}

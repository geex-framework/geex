using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Entities
{
    internal class StringValuesSerializer : SerializerBase<StringValues>
    {

        public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, StringValues stringValues)
        {
            if (stringValues == default(StringValues))
            {
                ctx.Writer.WriteNull();
            }
            else
            {
                ctx.Writer.WriteString(stringValues.ToString());
            }
        }

        public override StringValues Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
        {
            var bsonType = ctx.Reader.GetCurrentBsonType();

            switch (bsonType)
            {
                case BsonType.String:
                    return new StringValues(ctx.Reader.ReadString());
                default:
                    throw new FormatException($"Cannot deserialize a 'StringValues' from a [{bsonType}]");
            }
        }
    }
}

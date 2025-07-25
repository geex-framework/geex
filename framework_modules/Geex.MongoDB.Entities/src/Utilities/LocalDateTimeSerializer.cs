using System;

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Entities.Utilities
{
    public class LocalDateTimeSerializer : SerializerBase<DateTime>
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTime value)
        {
            if (value.Kind == DateTimeKind.Unspecified)
            {
                value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }
            var dateTimeOffset = new DateTimeOffset(value);
            context.Writer.WriteDateTime(dateTimeOffset.ToUnixTimeMilliseconds());
        }

        public override DateTime Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var millis = context.Reader.ReadDateTime();
            return DateTimeOffset.FromUnixTimeMilliseconds(millis).UtcDateTime;
        }
    }
}
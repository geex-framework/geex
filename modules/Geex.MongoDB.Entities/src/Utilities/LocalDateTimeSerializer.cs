using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Entities.Utilities
{
    public class LocalDateTimeSerializer : SerializerBase<DateTime>
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTime value)
        {
            BsonSerializer.LookupSerializer<object>().Serialize(context, args, value);
        }

        public override DateTime Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return ((DateTime)BsonSerializer.LookupSerializer<object>().Deserialize(context, args)).ToLocalTime();
        }
    }
}

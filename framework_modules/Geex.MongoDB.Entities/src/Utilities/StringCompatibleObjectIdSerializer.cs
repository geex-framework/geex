using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Entities.Utilities;

public class StringCompatibleObjectIdSerializer : ObjectIdSerializer
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ObjectId value)
    {
        var writer = context.Writer;
        if (value == default)
        {
            writer.WriteNull();
            return;
        }
        writer.WriteObjectId(value);
    }

    /// <inheritdoc />
    public override ObjectId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        if (reader.CurrentBsonType == BsonType.String)
        {
            var objectId = reader.ReadString();
            return ObjectId.Parse(objectId);
        }
        return base.Deserialize(context, args);
    }
}
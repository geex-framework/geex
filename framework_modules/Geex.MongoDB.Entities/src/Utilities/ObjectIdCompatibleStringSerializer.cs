using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Entities.Utilities;

public class ObjectIdCompatibleStringSerializer : StringSerializer
{

    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
    {
        var writer = context.Writer;
        if (value == default)
        {
            writer.WriteNull();
            return;
        }
        writer.WriteString(value);
    }

    /// <inheritdoc />
    public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        if (reader.CurrentBsonType == BsonType.ObjectId)
        {
            var objectId = reader.ReadObjectId();
            return objectId.ToString();
        }
        return base.Deserialize(context, args);
    }
}
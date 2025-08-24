using System;
using System.IO;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Entities.Utilities;

public class StringCompatibleObjectIdSerializer : SerializerBase<ObjectId>, IRepresentationConfigurable
{
    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ObjectId value)
    {
        var writer = context.Writer;
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        base.Serialize(context, args, value);
    }

    /// <inheritdoc />
    public override ObjectId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        return reader.CurrentBsonType switch
        {
            BsonType.String when ObjectId.TryParse(reader.ReadString(), out var objectId) => objectId,
            BsonType.ObjectId => base.Deserialize(context, args),
            _ => throw new InvalidDataException("")
        };
    }

    /// <inheritdoc />
    public BsonType Representation { get; } = BsonType.ObjectId;

    /// <inheritdoc />
    IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
    {
        return representation switch
        {
            BsonType.ObjectId => this,
            BsonType.String => BsonSerializer.LookupSerializer<string>(),
            _ => throw new InvalidDataException("")
        };
    }
}
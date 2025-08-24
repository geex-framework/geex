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
        context.Writer.WriteObjectId(value);
    }

    /// <inheritdoc />
    public override ObjectId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        return reader.CurrentBsonType switch
        {
            BsonType.String when ObjectId.TryParse(reader.ReadString(), out var objectId) => objectId,
            BsonType.ObjectId => reader.ReadObjectId(),
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
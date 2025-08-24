using System;
using System.IO;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Entities.Utilities;

public class ObjectIdCompatibleStringSerializer : SerializerBase<string>, IRepresentationConfigurable
{
    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string? value)
    {
        var writer = context.Writer;
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        if (ObjectId.TryParse(value, out var parsedObjectId))
        {
            writer.WriteObjectId(parsedObjectId);
            return;
        }

        writer.WriteString(value);
    }

    /// <inheritdoc />
    public override string? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        switch (reader.CurrentBsonType)
        {
            case BsonType.String:
                return reader.ReadString();
            case BsonType.ObjectId:
                return reader.ReadObjectId().ToString();
            case BsonType.Null:
                reader.ReadNull();
                return null;
            default:
                throw new InvalidDataException("");
        }
    }

    /// <inheritdoc />
    public BsonType Representation { get; } = BsonType.String;

    /// <inheritdoc />
    IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
    {
        return representation switch
        {
            BsonType.ObjectId => this,
            BsonType.String => this,
            _ => throw new InvalidDataException("")
        };
    }
}

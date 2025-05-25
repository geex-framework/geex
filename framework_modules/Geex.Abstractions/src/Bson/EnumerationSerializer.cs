using System;
using Geex.Abstractions;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Geex.Abstractions.Bson
{

    public class EnumerationSerializer<TEnum> :
        ClassSerializerBase<TEnum>,
        IRepresentationConfigurable, IEnumerationSerializer where TEnum : Enumeration<TEnum>
    {
        private readonly BsonType _representation = BsonType.String;
        private readonly TypeCode _underlyingTypeCode;

        public EnumerationSerializer()
        {

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="T:MongoDB.Bson.Serialization.Serializers.EnumSerializer`1" /> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        public EnumerationSerializer(BsonType representation = BsonType.String)
        {
            if (representation <= BsonType.String)
            {
                if (representation == BsonType.EndOfDocument || representation == BsonType.String)
                    goto label_4;
            }
            else if (representation == BsonType.Int32 || representation == BsonType.Int64)
                goto label_4;
            throw new ArgumentException(string.Format("{0} is not a valid representation for an EnumSerializer.", (object)representation), nameof(representation));
            label_4:
            if (!typeof(TEnum).IsClassEnum())
                throw new BsonSerializationException(string.Format("{0} is not an class enum type.", (object)typeof(TEnum).FullName));
            this._representation = representation;
            this._underlyingTypeCode = Type.GetTypeCode(typeof(string));
        }

        public IBsonSerializer WithRepresentation(BsonType representation)
        {
            return representation == this._representation ? this : new EnumerationSerializer<TEnum>(representation);
        }

        /// <summary>Gets the representation.</summary>
        /// <value>The representation.</value>
        public BsonType Representation => this._representation;

        /// <summary>Deserializes a value.</summary>
        /// <param name="context">The deserialization context.</param>
        /// <param name="args">The deserialization args.</param>
        /// <returns>A deserialized value.</returns>
        public override TEnum? Deserialize(
          BsonDeserializationContext context,
          BsonDeserializationArgs args)
        {
            IBsonReader reader = context.Reader;
            BsonType currentBsonType = reader.GetCurrentBsonType();
            object data;
            switch (currentBsonType)
            {
                case BsonType.Double:
                    data = reader.ReadDouble();
                    break;
                case BsonType.String:
                    data = reader.ReadString();
                    break;
                case BsonType.Int32:
                    data = reader.ReadInt32();
                    break;
                case BsonType.Int64:
                    data = reader.ReadInt64();
                    break;
                case BsonType.Null:
                    reader.ReadNull();
                    return default;
                default:
                    throw this.CreateCannotDeserializeFromBsonTypeException(currentBsonType);
            }
            var result = typeof(Enumeration<>).MakeGenericType(typeof(TEnum)).GetMethod(nameof(Enumeration.FromValue), types: new[] { typeof(string) })?.Invoke(null, new[] { data }) as TEnum;
            return result;
        }

        /// <summary>Serializes a value.</summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(
          BsonSerializationContext context,
          BsonSerializationArgs args,
          TEnum? value)
        {
            IBsonWriter writer = context.Writer;
            if (value == default)
            {
                writer.WriteNull();
                return;
            }
            switch (this._representation)
            {
                case BsonType.EndOfDocument:
                    if (this._underlyingTypeCode == TypeCode.Int64 || this._underlyingTypeCode == TypeCode.UInt64)
                        goto case BsonType.Int64;
                    else
                        goto case BsonType.Int32;
                case BsonType.String:
                    writer.WriteString((string)(object)value.Value);
                    break;
                case BsonType.Int32:
                    writer.WriteInt32((int)(object)value.Value);
                    break;
                case BsonType.Int64:
                    writer.WriteInt64((long)(object)value.Value);
                    break;
                default:
                    throw new BsonInternalException("Unexpected EnumRepresentation.");
            }
        }

    }
}

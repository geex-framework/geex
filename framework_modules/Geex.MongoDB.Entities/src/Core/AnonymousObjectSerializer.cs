//using System;
//using System.Collections.Generic;
//using System.Dynamic;
//using System.Globalization;
//using System.Text;
//using System.Text.Json;
//using MongoDB.Bson.Serialization.Serializers;

//using MongoDB.Bson.Serialization;

//using MongoDB.Bson;
//using MongoDB.Bson.IO;
//using SharpCompress.Writers;
//using System.Text.Json.Nodes;
//using SharpCompress.Readers;

//namespace Geex.MongoDB.Entities.Core
//{
//    public class AnonymousObjectBsonSerializer : ClassSerializerBase<object>,
//             IRepresentationConfigurable<AnonymousObjectBsonSerializer>
//    {
//        private BsonType _representation;

//        public AnonymousObjectBsonSerializer() : this(BsonType.Document)
//        {
//        }

//        public AnonymousObjectBsonSerializer(BsonType representation)
//        {
//            switch (representation)
//            {
//                case BsonType.Document:
//                    break;
//                default:
//                    throw new ArgumentException(
//                        $"{representation} is not a valid representation for {this.GetType().Name}");
//            }

//            _representation = representation;
//        }

//        public BsonType Representation => _representation;

//        public override object Deserialize(BsonDeserializationContext context,
//                                                   BsonDeserializationArgs args)
//        {
//            var bsonReader = context.Reader;
//            long ticks;
//            TimeSpan offset;

//            BsonType bsonType = bsonReader.GetCurrentBsonType();
//            switch (bsonType)
//            {
//                case BsonType.Document:
//                    {
//                        var bson = new BsonDocument();
//                        new BsonDocumentWriter(bson).WriteRawBsonDocument(bsonReader.ReadRawBsonDocument());
//                        var obj = JsonSerializer.Deserialize<ExpandoObject>(bson.ToJson());
//                        return obj;
//                    }

//                default:
//                    throw CreateCannotDeserializeFromBsonTypeException(bsonType);
//            }
//        }

//        public override void Serialize
//           (BsonSerializationContext context, BsonSerializationArgs args, object value)
//        {
//            var bsonWriter = context.Writer;

//            switch (_representation)
//            {
//                case BsonType.Document:
//                    {
//                        var bson = BsonDocument.Parse(JsonSerializer.Serialize(value));
//                        bsonWriter.WriteRawBsonDocument(new BsonDocumentReader(bson).ReadRawBsonDocument());
//                        break;
//                    }
//                default:
//                    var message = $"'{_representation}' is not a valid anonymous representation.";
//                    throw new BsonSerializationException(message);
//            }
//        }

//        public AnonymousObjectBsonSerializer WithRepresentation(BsonType representation)
//        {
//            if (representation == _representation)
//            {
//                return this;
//            }
//            return new AnonymousObjectBsonSerializer(representation);
//        }

//        IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
//        {
//            return WithRepresentation(representation);
//        }

//        protected Exception CreateCannotDeserializeFromBsonTypeException(BsonType bsonType)
//        {
//            var message =
//                $"Cannot deserialize a '{BsonUtils.GetFriendlyTypeName(ValueType)}' from BsonType '{bsonType}'.";
//            return new FormatException(message);
//        }
//    }
//}

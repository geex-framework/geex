//// The MIT License (MIT)
////
//// Copyright (c) 2015 Pathmatics, Inc
////
//// Permission is hereby granted, free of charge, to any person obtaining a copy
//// of this software and associated documentation files (the "Software"), to deal
//// in the Software without restriction, including without limitation the rights
//// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//// copies of the Software, and to permit persons to whom the Software is
//// furnished to do so, subject to the following conditions:
////
//// The above copyright notice and this permission notice shall be included in all
//// copies or substantial portions of the Software.
////
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//// SOFTWARE.

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using MongoDB.Bson;
//using MongoDB.Bson.Serialization;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Converters;
//using Newtonsoft.Json.Linq;

//namespace MongoLinqPlusPlus
//{
//    /// <summary>
//    /// Class to utilze the Mongo Bson Deserializer for a few custom types (DateTime, ObjectId, etc)
//    /// We also must use this for any clases which remap element names to properties/fields with the BsonElement or BsonId attributes.
//    /// </summary>
//    internal class MongoBsonConverter : CustomCreationConverter<object>
//    {
//        private readonly Dictionary<Type, bool> _useMongoBsonDeserializerDict = new Dictionary<Type, bool> {
//            {typeof(string), false},
//            {typeof(int), true},        // <-- Needs to be true to handle integer division.  See bugbug below
//            {typeof(uint), false},
//            {typeof(long), false},
//            {typeof(ulong), false},
//            {typeof(bool), false},
//            {typeof(byte), false},
//            {typeof(sbyte), false},
//            {typeof(char), false},
//            {typeof(short), false},
//            {typeof(ushort), false},
//            {typeof(double), false},
//            {typeof(DateTime), true},
//            {typeof(DateTime?), true},
//            {typeof(TimeSpan), true},       // Special code for this below
//            {typeof(ObjectId), true},
//            {typeof(Guid), true},
//        };

//        /// <summary>Cached collection of value types</summary>
//        private Dictionary<Type, bool> _valueTypeDict = new Dictionary<Type, bool>();

//        /// <summary>
//        /// We can convert (return true) for any supplied type which has a property or field with one
//        /// of the MongoDB.Bson attributes (BsonElement, BsonId, etc).  Json.Net is not aware of these
//        /// attributes and will fail to correctly map names
//        /// </summary>
//        public override bool CanConvert(Type objectType)
//        {
//            if (_useMongoBsonDeserializerDict.TryGetValue(objectType, out bool canConvert))
//                return canConvert;

//            // Don't bother for anonymous types
//            if (!objectType.IsAnonymousType())
//            {
//                // Get each attribute for each field and property
//                foreach (var member in objectType.GetFields().Cast<MemberInfo>().Concat(objectType.GetProperties()))
//                foreach (var attribute in member.GetCustomAttributes(true))
//                {
//                    string attributeName = attribute.ToString();
//                    if (attributeName.StartsWith("MongoDB.Bson"))
//                    {
//                        _useMongoBsonDeserializerDict.Add(objectType, true);
//                        return true;
//                    }
//                }
//            }

//            _useMongoBsonDeserializerDict.Add(objectType, false);
//            return false;
//        }

//        /// <summary>Required override</summary>
//        public override object Create(Type objectType) => throw new NotImplementedException("Looks like we don't actually need this...");

//        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
//        {
//            if (objectType == typeof(int))
//            {
//                if (reader.TokenType == JsonToken.Float)
//                {
//                    // BUGBUG!  The MongoDB documentation for $trunc specifies that it returns an integer.  However, we're actually
//                    // getting a type of double back from the server (ableit with no decimal part).  So we need to deserialize this ourselves.
//                    return (int) (double) reader.Value;
//                }

//                if (reader.TokenType != JsonToken.Integer)
//                    throw new InvalidCastException("No explicit conversion of JsonToken type " + reader.TokenType + " to Int32 implemented.");

//                // TODO: Bounds check the long?
//                return (int) (long) reader.Value;
//            }

//            // Our timespans are actually in our json stream as integers (100-nano chunks).
//            // Our reader will bomb out reading the this object because it isn't expecting an Integer token.
//            // So we'll just handle this type ourselves and not delegate to the bson deserializer.
//            if (objectType == typeof(TimeSpan))
//            {
//                if (reader.TokenType != JsonToken.Integer)
//                    throw new InvalidOperationException($"Json deserializer can't deserialize TimeSpan from token type {reader.TokenType}.  (Expected token type Integer.)");

//                return new TimeSpan((long) reader.Value);
//            }

//            // If we're deserializing null, then there's no real deserialization needed.
//            if (reader.TokenType == JsonToken.Null)
//            {
//                if (!objectType.IsValueType)
//                    return null;

//                if (!_valueTypeDict.TryGetValue(objectType, out bool isValueType))
//                {
//                    isValueType = objectType.IsNonNullableValueType();
//                    _valueTypeDict[objectType] = isValueType;
//                }

//                if (isValueType)
//                    throw new NullReferenceException("Error deserialzing null into value type " + objectType.Name);

//                return null;
//            }

//            var jObject = JObject.Load(reader);
//            string json = jObject.ToString();
//            return BsonSerializer.Deserialize(json, objectType);
//        }
//    }
//}


using System.Collections.Generic;
using MongoDB.Bson.Serialization;

namespace Geex
{
    public static class MongoDB_Bson_Extensions
    {
        public static IEnumerable<BsonClassMap> GetRegisteredClassMaps<T>(this BsonClassMap<T> bsonClassMap) => BsonClassMap.GetRegisteredClassMaps();

    }
}

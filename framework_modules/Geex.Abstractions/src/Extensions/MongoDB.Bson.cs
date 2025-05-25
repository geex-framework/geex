using System.Collections.Generic;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson
{
    public static class MongoDB_Bson_Extensions
    {
        public static IEnumerable<BsonClassMap> GetRegisteredClassMaps<T>(this BsonClassMap<T> bsonClassMap) => BsonClassMap.GetRegisteredClassMaps();

    }
}

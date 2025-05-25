using System;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Geex.Bson
{
    public class EnumerationRepresentationConvention : ConventionBase, IMemberMapConvention
    {
        public void Apply(BsonMemberMap memberMap)
        {
            if (memberMap.MemberType.IsAssignableTo<IEnumeration>())
            {
                var serializerType = typeof(EnumerationSerializer<>).MakeGenericType(memberMap.MemberType);
                var serializer = Activator.CreateInstance(serializerType);
                memberMap.SetSerializer(serializer as IBsonSerializer);
            }
        }
    }
}

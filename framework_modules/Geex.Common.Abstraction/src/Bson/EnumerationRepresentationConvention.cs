using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstractions;

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Geex.Common.Abstraction.Bson
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

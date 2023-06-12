using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities
{
    internal class InterceptedAndFiltered : EntityBase<InterceptedAndFiltered>, IIntercepted
    {
        public int Value { get; set; }
    }
}
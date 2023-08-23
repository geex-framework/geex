using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities
{
    internal class InterceptedAndFiltered : EntityBase<InterceptedAndFiltered>, ISaveIntercepted, IAttachIntercepted
    {
        public int Value { get; set; }

        /// <inheritdoc />
        public async Task InterceptOnSave()
        {
            this.Value += 1;
        }

        /// <inheritdoc />
        public void InterceptOnAttached()
        {
            this.Value += 1;
        }
    }
}
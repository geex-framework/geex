using System.Threading.Tasks;
using MongoDB.Entities.Interceptors;

namespace MongoDB.Entities
{
    internal class InterceptedAndFiltered : EntityBase<InterceptedAndFiltered>, ISaveIntercepted, IAttachIntercepted
    {
        public int Value { get; set; }

        /// <inheritdoc />
        public void InterceptOnAttached()
        {
            this.Value += 1;
        }

        /// <inheritdoc />
        public async Task InterceptOnSave(IEntityBase originalValue)
        {
            this.Value += 1;
        }
    }
}
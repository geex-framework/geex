using System.Threading.Tasks;

namespace MongoDB.Entities.Interceptors
{
    public interface IAttachIntercepted : IEntityBase
    {
        public void InterceptOnAttach();
    }

    public interface ISaveIntercepted : IEntityBase
    {
        public Task InterceptOnSave();
    }
}
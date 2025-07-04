using System.Threading.Tasks;
using Geex.Extensions.BlobStorage.Requests;
using Geex.Gql.Types;

namespace Geex.Extensions.BlobStorage.Gql
{
    public sealed class BlobObjectMutation : MutationExtension<BlobObjectMutation>
    {
        private readonly IUnitOfWork _uow;

        public BlobObjectMutation(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        /// <summary>
        /// 创建BlobObject
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IBlobObject> CreateBlobObject(CreateBlobObjectRequest request) => await _uow.Request(request);

        /// <summary>
        /// 删除BlobObject
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> DeleteBlobObject(
            DeleteBlobObjectRequest request)
        {
            await _uow.Request(request);
            return true;
        }
    }
}

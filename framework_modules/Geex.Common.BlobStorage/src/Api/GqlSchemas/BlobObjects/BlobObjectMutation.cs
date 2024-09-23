using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Gql.Types;

using Geex.Common.Requests.BlobStorage;

using HotChocolate.Types;

using MediatR;

namespace Geex.Common.BlobStorage.Api.GqlSchemas.BlobObjects
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

using System.Threading.Tasks;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Gql.Types;

using Geex.Common.Requests.BlobStorage;
using MediatR;

namespace Geex.Common.BlobStorage.Api.GqlSchemas.BlobObjects
{
    public class BlobObjectMutation : MutationExtension<BlobObjectMutation>
    {
        private readonly IMediator _mediator;

        public BlobObjectMutation(IMediator mediator)
        {
            this._mediator = mediator;
        }

        /// <summary>
        /// 创建BlobObject
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IBlobObject> CreateBlobObject(
            CreateBlobObjectRequest request)
        {
            var result = await _mediator.Send(request);
            return result;
        }

        /// <summary>
        /// 删除BlobObject
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> DeleteBlobObject(
            DeleteBlobObjectRequest request)
        {
            await _mediator.Send(request);
            return true;
        }
    }
}

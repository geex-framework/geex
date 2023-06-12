using System.Threading.Tasks;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.BlobStorage.Api.Aggregates.BlobObjects;
using Geex.Common.BlobStorage.Api.Aggregates.BlobObjects.Inputs;
using HotChocolate;
using HotChocolate.Types;

using MediatR;

using MongoDB.Entities;

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
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IBlobObject> CreateBlobObject(
            CreateBlobObjectRequest input)
        {
            var result = await _mediator.Send(input);
            return result;
        }

        /// <summary>
        /// 删除BlobObject
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> DeleteBlobObject(
            DeleteBlobObjectRequest input)
        {
            var result = await _mediator.Send(input);
            return true;
        }
    }
}

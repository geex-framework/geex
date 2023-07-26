using Geex.Common.Abstraction.Entities;
using HotChocolate.Types;
using MediatR;

namespace Geex.Common.BlobStorage.Api.Aggregates.BlobObjects.Inputs
{
    public class CreateBlobObjectRequest : IRequest<IBlobObject>
    {
        public IFile File { get; set; }
        public BlobStorageType StorageType { get; set; }
        /// <summary>
        /// can pass null, will be calculated
        /// </summary>
        public string? Md5 { get; set; }
    }
}

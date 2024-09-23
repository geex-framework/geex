using Geex.Common.Abstraction.Entities;
using HotChocolate.Types;
using MediatR;

namespace Geex.Common.Requests.BlobStorage
{
    public record CreateBlobObjectRequest : IRequest<IBlobObject>
    {
        public IFile File { get; set; }
        public BlobStorageType StorageType { get; set; }
        /// <summary>
        /// can pass null, will be calculated
        /// </summary>
        public string? Md5 { get; set; }
    }
}

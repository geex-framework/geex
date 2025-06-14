
using HotChocolate.Types;
using MediatX;

namespace Geex.Extensions.BlobStorage.Requests
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

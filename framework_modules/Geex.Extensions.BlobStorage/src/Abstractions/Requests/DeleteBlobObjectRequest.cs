using System.Collections.Generic;

using MediatX;

namespace Geex.Extensions.BlobStorage.Requests
{
    public record DeleteBlobObjectRequest : IRequest
    {
        public List<string> Ids { get; set; }
        public BlobStorageType StorageType { get; set; }
    }
}

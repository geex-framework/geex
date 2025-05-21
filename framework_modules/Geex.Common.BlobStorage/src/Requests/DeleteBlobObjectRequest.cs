using System.Collections.Generic;
using Geex.Common.Abstraction.Entities;
using MediatR;

namespace Geex.Common.BlobStorage.Requests
{
    public record DeleteBlobObjectRequest : IRequest
    {
        public List<string> Ids { get; set; }
        public BlobStorageType StorageType { get; set; }
    }
}

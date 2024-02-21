
using System.Collections.Generic;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstractions;
using MediatR;

namespace Geex.Common.BlobStorage.Api.Aggregates.BlobObjects.Inputs
{
    public class DeleteBlobObjectRequest : IRequest
    {
        public List<string> Ids { get; set; }
        public BlobStorageType StorageType { get; set; }
    }
}

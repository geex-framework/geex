using System.IO;
using Geex.Common.Abstraction.Entities;
using MediatR;

namespace Geex.Common.BlobStorage.Api.Abstractions
{
    public class DownloadFileRequest : IRequest<(IBlobObject blob, Stream dataStream)>
    {
        public DownloadFileRequest(string blobId, BlobStorageType storageType)
        {
            BlobId = blobId;
            StorageType = storageType;
        }

        public string BlobId { get; set; }
        public BlobStorageType StorageType { get; set; }
    }
}

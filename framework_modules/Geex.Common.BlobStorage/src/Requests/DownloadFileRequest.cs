using System.IO;

using Geex.Common.Abstraction.Entities;

using MediatR;

namespace Geex.Common.BlobStorage.Requests
{
    public record DownloadFileRequest : IRequest<(IBlobObject blob, Stream dataStream)>
    {
        public DownloadFileRequest(string blobId)
        {
            BlobId = blobId;
        }

        public string BlobId { get; set; }
    }
}

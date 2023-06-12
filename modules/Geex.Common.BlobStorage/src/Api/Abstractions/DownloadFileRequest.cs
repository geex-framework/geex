using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Entities;
using Geex.Common.BlobStorage.Api.Aggregates.BlobObjects;
using Geex.Common.BlobStorage.Core.Aggregates.BlobObjects;
using MediatR;
using Microsoft.Extensions.Primitives;

namespace Geex.Common.BlobStorage.Api.Abstractions
{
    public class DownloadFileRequest : IRequest<(IBlobObject blob, DbFile dbFile)>
    {
        public DownloadFileRequest(string fileId, BlobStorageType storageType)
        {
            FileId = fileId;
            StorageType = storageType;
        }

        public string FileId { get; set; }
        public BlobStorageType StorageType { get; set; }
    }
}

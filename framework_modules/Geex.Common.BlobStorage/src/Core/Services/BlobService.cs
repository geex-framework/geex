using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Geex.Common.BlobStorage.Api.Abstractions;
using Geex.Common.BlobStorage.Core.Handlers;
using Geex.Common.Requests.BlobStorage;
using HotChocolate.Types;

namespace Geex.Common.BlobStorage.Core.Services
{
    /// <summary>
    /// Implementation of the IBlobStorageService interface
    /// </summary>
    public class BlobService : IBlobService
    {
        private readonly IUnitOfWork _uow;

        public BlobService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        /// <inheritdoc />
        public async Task<IBlobObject> CreateBlobAsync(IFile file, BlobStorageType storageType, string? md5 = null, CancellationToken cancellationToken = default)
        {
            var request = new CreateBlobObjectRequest
            {
                File = file,
                StorageType = storageType,
                Md5 = md5
            };
            
            return await _uow.Request(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task DeleteBlobsAsync(List<string> ids, BlobStorageType storageType, CancellationToken cancellationToken = default)
        {
            var request = new DeleteBlobObjectRequest
            {
                Ids = ids,
                StorageType = storageType
            };
            
            await _uow.Request(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<(IBlobObject blob, Stream dataStream)> DownloadBlobAsync(string blobId, CancellationToken cancellationToken = default)
        {
            var request = new DownloadFileRequest(blobId);
            
            return await _uow.Request(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IBlobObject> GetBlobAsync(string blobId)
        {
            return await Task.FromResult(_uow.Query<IBlobObject>().FirstOrDefault(x => x.Id == blobId));
        }

        /// <inheritdoc />
        public int GetBufferSize(long fileLength)
        {
            return BlobObjectHandler.GetBufferSize(fileLength);
        }
    }
}
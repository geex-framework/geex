using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Entities;
using HotChocolate.Types;

namespace Geex.Common.BlobStorage.Api.Abstractions
{
    /// <summary>
    /// Service interface for managing blob storage operations
    /// </summary>
    public interface IBlobService
    {
        /// <summary>
        /// Creates a blob object from a file
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <param name="storageType">The storage type (DB, FileSystem, Cache)</param>
        /// <param name="md5">Optional MD5 hash if pre-calculated</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created blob object</returns>
        Task<IBlobObject> CreateBlobAsync(IFile file, BlobStorageType storageType, string? md5 = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes blob objects by their IDs
        /// </summary>
        /// <param name="ids">IDs of the blobs to delete</param>
        /// <param name="storageType">Storage type of the blobs</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task DeleteBlobsAsync(List<string> ids, BlobStorageType storageType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a blob object by its ID
        /// </summary>
        /// <param name="blobId">ID of the blob to download</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tuple containing the blob object and its data stream</returns>
        Task<(IBlobObject blob, Stream dataStream)> DownloadBlobAsync(string blobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an existing blob object by ID
        /// </summary>
        /// <param name="blobId">ID of the blob to retrieve</param>
        /// <returns>Blob object</returns>
        Task<IBlobObject> GetBlobAsync(string blobId);

        /// <summary>
        /// Gets the appropriate buffer size based on file size
        /// </summary>
        /// <param name="fileLength">Length of the file in bytes</param>
        /// <returns>Buffer size in bytes</returns>
        int GetBufferSize(long fileLength);
    }
}
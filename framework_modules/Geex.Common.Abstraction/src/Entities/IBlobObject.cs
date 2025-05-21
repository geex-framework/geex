using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Entities;

namespace Geex.Common.Abstraction.Entities
{
    /// <summary>
    /// Represents a blob object in the storage system
    /// </summary>
    public interface IBlobObject : IEntityBase
    {
        /// <summary>
        /// Name of the file
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// MD5 hash of the file content
        /// </summary>
        public string? Md5 { get; set; }

        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        public long FileSize { get; }

        /// <summary>
        /// MIME type of the file
        /// </summary>
        public string? MimeType { get; }

        /// <summary>
        /// URL to access the file
        /// </summary>
        public string? Url { get; }

        /// <summary>
        /// Storage type used for this blob
        /// </summary>
        public BlobStorageType? StorageType { get; }

        /// <summary>
        /// Expiration time for the blob (if applicable)
        /// </summary>
        public DateTimeOffset? ExpireAt { get; set; }

        /// <summary>
        /// Gets the absolute file path in the file system (for FileSystem storage type)
        /// </summary>
        public string GetFilePath();

        /// <summary>
        /// Streams the blob data from storage
        /// </summary>
        public Task<Stream> StreamFromStorage(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves a stream to the blob storage
        /// </summary>
        Task StreamToStorage(Stream dataStream, CancellationToken cancellationToken = default);
    }
}

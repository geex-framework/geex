using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Geex.Common.BlobStorage.Api;
using Geex.Common.BlobStorage.Core.Aggregates.BlobObjects;
using Geex.Common.BlobStorage.Api.Abstractions;
using Geex.Common.Requests.BlobStorage;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MimeKit;
using MongoDB.Entities;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;
using SharpCompress.Compressors.Xz;
using System.Reflection.Metadata;

namespace Geex.Common.BlobStorage.Core.Handlers
{
    public class BlobObjectHandler :
        ICommonHandler<IBlobObject, BlobObject>,
        IRequestHandler<CreateBlobObjectRequest, IBlobObject>,
        IRequestHandler<DeleteBlobObjectRequest>,
        IRequestHandler<DownloadFileRequest, (IBlobObject blob, Stream dataStream)>
    {
        private readonly IMemoryCache _memCache;
        private readonly IRedisDatabase _redis;
        private readonly BlobStorageModuleOptions _options;
        public IUnitOfWork Uow { get; }

        public BlobObjectHandler(IUnitOfWork uow, IMemoryCache memCache, IRedisDatabase redis, BlobStorageModuleOptions options)
        {
            _memCache = memCache;
            _redis = redis;
            _options = options;
            Uow = uow;
        }

        public virtual async Task<IBlobObject> Handle(CreateBlobObjectRequest request, CancellationToken cancellationToken)
        {
            // Open the file stream
            await using var readStream = request.File.OpenReadStream();

            // Determine buffer size based on file size
            var fileLength = request.File.Length;

            var bufferSize = fileLength switch
            {
                < 1048576L => 81920,
                < 10485760L => 1048576,
                < 104857600L => 2097152,
                >= 104857600L => 4194304,
            };

            var buffer = new byte[bufferSize];

            // Create MD5 hash object
            using var md5Hasher = MD5.Create();

            // Initialize blob object
            var entity = new BlobObject(
                request.File.Name,
                null,
                request.StorageType,
                request.File.ContentType ?? MimeTypes.GetMimeType(request.File.Name),
                fileLength.Value
            );
            entity = Uow.Attach(entity);

            // Process based on storage type
            if (request.StorageType == BlobStorageType.Db)
            {
                var dbFile = Uow.Query<DbFile>().FirstOrDefault(x => x.Md5 == request.Md5);
                if (dbFile == null)
                {
                    dbFile = new DbFile(null); // Delay setting MD5
                    dbFile = Uow.Attach(dbFile);

                    await dbFile.Data.UploadAsync(readStream, bufferSize / 1024, cancellationToken, md5Hasher);
                    // Write to database and compute MD5
                    await ProcessStreamAsync(readStream, buffer, md5Hasher,
                        async (chunk, length) =>
                        {
                            // Write chunk to dbFile.Data

                        }, cancellationToken);
                }
            }
            else if (request.StorageType == BlobStorageType.Cache)
            {
                if (fileLength <= 512 * 1024 * 1024) // Cache only files <= 512 GB
                {
                    var memoryStream = new MemoryStream();

                    // Write to memory stream and compute MD5
                    await ProcessStreamAsync(readStream, buffer, md5Hasher,
                        async (chunk, length) =>
                        {
                            await memoryStream.WriteAsync(chunk.AsMemory(0, length), cancellationToken);
                        }, cancellationToken);

                    // Set cache
                    _memCache.Set(entity.Md5, memoryStream, TimeSpan.FromMinutes(5));
                    await _redis.SetNamedAsync(entity, expireIn: TimeSpan.FromMinutes(5), token: cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException("over sized file for cache!");
                }
            }
            else if (request.StorageType == BlobStorageType.FileSystem)
            {
                // For file system storage, write directly to file
                var filePath = GetFilePath(request.Md5);
                if (!File.Exists(filePath))
                {
                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    await using var fileStream = new FileStream(
                        filePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        buffer.Length,
                        useAsync: true
                    );

                    // Write to file and compute MD5
                    await ProcessStreamAsync(readStream, buffer, md5Hasher,
                        async (chunk, length) =>
                        {
                            await fileStream.WriteAsync(chunk.AsMemory(0, length), cancellationToken);
                        }, cancellationToken);
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            // Finalize MD5 hash computation
            md5Hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            request.Md5 = BitConverter.ToString(md5Hasher.Hash).Replace("-", "").ToLowerInvariant();
            entity.Md5 = request.Md5;

            return entity;
        }

        // Process stream method
        private async Task ProcessStreamAsync(
            Stream inputStream,
            byte[] buffer,
            MD5 md5Hasher,
            Func<byte[], int, Task> processChunk,
            CancellationToken cancellationToken)
        {
            int bytesRead;
            while ((bytesRead = await inputStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                // Update MD5 hash
                md5Hasher.TransformBlock(buffer, 0, bytesRead, null, 0);

                // Process chunk
                await processChunk(buffer, bytesRead);
            }
        }

        private string GetFilePath(string md5)
        {
            var fileSystemStoragePath = this._options.FileSystemStoragePath;
            return Path.Combine(fileSystemStoragePath, md5);
        }

        public virtual async Task Handle(DeleteBlobObjectRequest request, CancellationToken cancellationToken)
        {
            if (request.StorageType == BlobStorageType.Db)
            {
                var blobObjects = await Task.FromResult(Uow.Query<BlobObject>().Where(x => request.Ids.Contains(x.Id)));
                foreach (var blobObject in blobObjects)
                {
                    var duplicateCount = await Uow.DbContext.CountAsync<BlobObject>(x => x.Md5 == blobObject.Md5, cancellationToken);
                    if (duplicateCount <= 1)
                    {
                        var filePath = GetFilePath(blobObject.Md5); ;
                        if (!File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                }
                await blobObjects.DeleteAsync();
                return;
            }
            else if (request.StorageType == BlobStorageType.FileSystem)
            {
                var blobObjects = await Task.FromResult(Uow.Query<BlobObject>().Where(x => request.Ids.Contains(x.Id)));
                foreach (var blobObject in blobObjects)
                {
                    var duplicateCount = await Uow.DbContext.CountAsync<BlobObject>(x => x.Md5 == blobObject.Md5, cancellationToken);
                    if (duplicateCount <= 1)
                    {
                        var dbFile = await Task.FromResult(Uow.Query<DbFile>().Single(x => x.Md5 == blobObject.Md5));
                        await dbFile.Data.ClearAsync(cancellationToken);
                        await dbFile.DeleteAsync();
                    }
                }
                await blobObjects.DeleteAsync();
                return;
            }
            throw new NotImplementedException();
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<(IBlobObject blob, Stream dataStream)> Handle(DownloadFileRequest request, CancellationToken cancellationToken)
        {
            Stream dataStream;
            if (request.StorageType == BlobStorageType.Db)
            {
                var blob = await Task.FromResult(Uow.Query<BlobObject>().First(x => x.Id == request.BlobId));
                var dbFile = await Task.FromResult(Uow.Query<DbFile>().First(x => x.Md5 == blob.Md5));

                // 直接使用数据库提供的流而不是将其复制到内存中
                dataStream = await dbFile.Data.DownloadAsStreamAsync(10, cancellationToken);
                return (blob, dataStream);
            }
            else if (request.StorageType == BlobStorageType.Cache)
            {
                var blob = await _redis.GetNamedAsync<BlobObject>(request.BlobId);

                // 从缓存中直接获取 Stream，避免内存复制
                var cachedStream = _memCache.Get<Stream>(blob.Md5);
                if (cachedStream != null)
                {
                    return (blob, cachedStream);
                }
                else
                {
                    // 缓存中没有对应数据时的处理逻辑
                    throw new FileNotFoundException("File not found in cache.");
                }
            }
            else if (request.StorageType == BlobStorageType.FileSystem)
            {
                var blob = await Task.FromResult(Uow.Query<BlobObject>().First(x => x.Id == request.BlobId));
                var filePath = GetFilePath(blob.Md5);

                // 直接返回文件的 FileStream，不再使用 MemoryStream 缓存文件内容
                dataStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                return (blob, dataStream);
            }
            throw new NotImplementedException();
        }

    }
}

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
using Geex.Common.Abstractions;
using HotChocolate.Types;
using MongoDB.Bson;
using SharpCompress.Common;
using Microsoft.AspNetCore.Http;

namespace Geex.Common.BlobStorage.Core.Handlers
{
    public class BlobObjectHandler :
        ICommonHandler<IBlobObject, BlobObject>,
        IRequestHandler<CreateBlobObjectRequest, IBlobObject>,
        IRequestHandler<DeleteBlobObjectRequest>,
        IRequestHandler<DownloadFileRequest, (IBlobObject blob, Stream dataStream)>
    {
        private readonly IMemoryCache _memCache;
        private readonly BlobStorageModuleOptions _options;
        private readonly IRedisDatabase _redis;

        public BlobObjectHandler(IUnitOfWork uow, IMemoryCache memCache, IRedisDatabase redis, BlobStorageModuleOptions options)
        {
            _memCache = memCache;
            _redis = redis;
            _options = options;
            Uow = uow;
        }

        public IUnitOfWork Uow { get; }

        public virtual async Task<IBlobObject> Handle(CreateBlobObjectRequest request, CancellationToken cancellationToken)
        {
            // 打开文件流
            await using var readStream = request.File.OpenReadStream();

            // 确定缓冲区大小
            var fileSize = request.File.Length ?? readStream.Length;
            var fileName = readStream is FileStream fs ? fs.Name : request.File.Name;
            var bufferSize = GetBufferSize(fileSize);

            // 确定文件内容类型
            var fileContentType = GetContentType(request.File, readStream);

            // 初始化 BlobObject
            // 处理文件存储
            if (request.StorageType == BlobStorageType.Db)
                return await HandleDbStorageAsync(request.Md5, fileName, fileContentType, fileSize, readStream, bufferSize, cancellationToken);
            else if (request.StorageType == BlobStorageType.Cache)
                return await HandleCacheStorageAsync(request.Md5, fileName, fileContentType, fileSize, readStream, bufferSize, cancellationToken);
            else if (request.StorageType == BlobStorageType.FileSystem)
                return await HandleFileSystemStorageAsync(request.Md5, fileName, fileContentType, fileSize, readStream, bufferSize, cancellationToken);
            else
                throw new NotImplementedException();
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
                if (blob.Md5.IsNullOrEmpty())
                {
                    await RemoveInvalidBlob(cancellationToken, blob);
                }

                var dbFile = await Task.FromResult(Uow.Query<DbFile>().FirstOrDefault(x => x.Md5 == blob.Md5));
                if (dbFile == default)
                {
                    await RemoveInvalidBlob(cancellationToken, blob);
                }
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
                if (blob.Md5.IsNullOrEmpty())
                {
                    await RemoveInvalidBlob(cancellationToken, blob);
                }
                var filePath = GetFilePath(blob.Md5);

                if (!File.Exists(filePath))
                {
                    await RemoveInvalidBlob(cancellationToken, blob);
                }

                // 直接返回文件的 FileStream，不再使用 MemoryStream 缓存文件内容
                dataStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                return (blob, dataStream);
            }
            throw new NotImplementedException();
        }

        // 获取缓冲区大小
        private int GetBufferSize(long fileLength)
        {
            return fileLength switch
            {
                < 2097152L => 262144,
                < 8388608L => 1048576,
                < 16777216L => 2097152,
                _ => 4194304,
            };
        }

        // 获取文件内容类型
        private string GetContentType(IFile file, Stream readStream)
        {
            if (!string.IsNullOrEmpty(file.ContentType))
                return file.ContentType;

            if (!string.IsNullOrEmpty(file.Name))
                return MimeTypes.GetMimeType(file.Name);

            if (readStream is FileStream fs)
                return MimeTypes.GetMimeType(fs.Name);

            return "application/octet-stream";
        }

        private string GetFilePath(string md5)
        {
            var fileSystemStoragePath = this._options.FileSystemStoragePath;
            return Path.Combine(fileSystemStoragePath, md5);
        }

        // 处理缓存存储
        private async Task<BlobObject> HandleCacheStorageAsync(string? md5, string fileName,
            string fileContentType,
            long fileSize, Stream readStream, int bufferSize, CancellationToken cancellationToken)
        {
            if (!md5.IsNullOrEmpty())
            {
                var existed = await _redis.GetNamedAsync<BlobObject>(md5);
                if (existed != null)
                {
                    return existed;
                }
            }
            var entity = new BlobObject(fileName, null, BlobStorageType.Cache, fileContentType, fileSize);
            if (fileSize > 512 * 1024 * 1024)
                throw new InvalidOperationException("文件大小超过缓存限制！");

            using var memoryStream = new MemoryStream();
            using var md5Hasher = MD5.Create();

            var md5Hash = await ProcessStreamAsync(readStream, bufferSize, md5Hasher, async (chunk, length) =>
            {
                await memoryStream.WriteAsync(chunk.AsMemory(0, length), cancellationToken);
            }, cancellationToken);

            entity.Md5 = md5Hash;
            // 设置缓存
            _memCache.Set(entity.Md5, memoryStream.ToArray(), TimeSpan.FromMinutes(5));
            await _redis.SetNamedAsync(entity, keyOverride: entity.Md5, expireIn: TimeSpan.FromMinutes(5), token: cancellationToken);
            return entity;
        }

        // 处理数据库存储
        private async Task<BlobObject> HandleDbStorageAsync(string? md5, string fileName,
            string fileContentType,
            long fileSize, Stream readStream, int bufferSize, CancellationToken cancellationToken)
        {
            if (!md5.IsNullOrEmpty())
            {
                var existed = Uow.Query<BlobObject>().FirstOrDefault(x => x.Md5 == md5 && x.StorageType == BlobStorageType.Db);
                if (existed != default)
                {
                    if (existed.FileName == fileName && existed.MimeType == fileContentType)
                    {
                        return existed;
                    }

                    if (Uow.Query<DbFile>().Any(x => x.Md5 == md5))
                    {
                        var reuseEntity = Uow.Attach(new BlobObject(fileName, md5, BlobStorageType.FileSystem, fileContentType, fileSize));
                        return reuseEntity;
                    }
                }
            }
            var entity = Uow.Attach(new BlobObject(fileName, default, BlobStorageType.Db, fileContentType, fileSize));
            var dbFile = Uow.Attach(new DbFile(null));

            // 使用 MD5 哈希算法
            using var md5Hasher = MD5.Create();
            // 上传数据并计算 MD5
            await dbFile.Data.UploadAsync(readStream, bufferSize / 1024, cancellationToken, md5Hasher);
            // 完成 MD5 计算
            md5Hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var md5Hash = BitConverter.ToString(md5Hasher.Hash).Replace("-", "").ToLowerInvariant();
            entity.Md5 = md5Hash;
            dbFile.Md5 = md5Hash;

            return entity;
        }

        // 处理文件系统存储
        private async Task<BlobObject> HandleFileSystemStorageAsync(string? md5,
            string fileName,
            string fileContentType, long fileSize, Stream readStream, int bufferSize,
            CancellationToken cancellationToken)
        {
            if (!md5.IsNullOrEmpty())
            {
                var existed = Uow.Query<BlobObject>().FirstOrDefault(x => x.Md5 == md5 && x.StorageType == BlobStorageType.FileSystem);

                if (existed != null)
                {
                    if (existed.FileName == fileName && existed.MimeType == fileContentType)
                    {
                        return existed;
                    }
                }

                if (File.Exists(GetFilePath(md5)))
                {
                    var reuseEntity = Uow.Attach(new BlobObject(fileName, md5, BlobStorageType.FileSystem, fileContentType, fileSize));
                    return reuseEntity;
                }
            }

            // 以下是处理新的 BlobObject 的代码
            var newEntity = Uow.Attach(new BlobObject(fileName, null, BlobStorageType.FileSystem, fileContentType, fileSize));

            var tempFileName = ObjectId.GenerateNewId().ToString();
            var tempFilePath = GetFilePath(tempFileName);

            using var md5Hasher = MD5.Create();

            await using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true);

            var md5Hash = await ProcessStreamAsync(readStream, bufferSize, md5Hasher, async (chunk, length) =>
            {
                await fileStream.WriteAsync(chunk.AsMemory(0, length), cancellationToken);
            }, cancellationToken);

            var finalFilePath = GetFilePath(md5Hash);
            // 关闭文件流并重命名临时文件
            await fileStream.DisposeAsync().ConfigureAwait(false);
            File.Move(tempFilePath, finalFilePath, true);

            newEntity.Md5 = md5Hash;
            return newEntity;

        }

        // 处理流并计算 MD5
        private async Task<string> ProcessStreamAsync(Stream inputStream, int bufferSize, MD5 md5Hasher, Func<byte[], int, Task> writeAction, CancellationToken cancellationToken)
        {
            var buffer = new byte[bufferSize];
            int bytesRead;

            while ((bytesRead = await inputStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                md5Hasher.TransformBlock(buffer, 0, bytesRead, null, 0);
                if (writeAction != null)
                {
                    await writeAction(buffer, bytesRead);
                }
            }

            md5Hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return BitConverter.ToString(md5Hasher.Hash).Replace("-", "").ToLowerInvariant();
        }

        private async Task RemoveInvalidBlob(CancellationToken cancellationToken, BlobObject blob)
        {
            await blob.DeleteAsync();
            await Uow.SaveChanges(cancellationToken);
            throw new BusinessException("File is corrupted, please try upload again.");
        }
    }
}

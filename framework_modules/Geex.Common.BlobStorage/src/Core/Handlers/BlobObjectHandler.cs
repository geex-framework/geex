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
            var stream = new MemoryStream();
            await using (var readStream = request.File.OpenReadStream())
            {
                await readStream.CopyToAsync(stream, cancellationToken);
                stream.Position = 0;
            }
            request.Md5 ??= stream.Md5();
            var md5 = request.Md5;
            var entity = new BlobObject(request.File.Name, md5, request.StorageType, request.File.ContentType ?? MimeTypes.GetMimeType(request.File.Name), stream.Length);
            entity = Uow.Attach(entity);
            // todo: fix duplicate md5 issue
            if (request.StorageType == BlobStorageType.Db)
            {
                var dbFile = Uow.Query<DbFile>().FirstOrDefault(x => x.Md5 == md5);
                if (dbFile == null)
                {
                    dbFile = new DbFile(entity.Md5);
                    dbFile = Uow.Attach(dbFile);
                    await dbFile.Data.UploadAsync(stream, cancellation: cancellationToken);
                }
            }
            else if (request.StorageType == BlobStorageType.Cache)
            {
                _memCache.Set(entity.Md5, stream, TimeSpan.FromMinutes(5));
                await _redis.SetNamedAsync(entity, expireIn: TimeSpan.FromMinutes(5), token: cancellationToken);
            }
            else if (request.StorageType == BlobStorageType.FileSystem)
            {
                var filePath = GetFilePath(md5);
                if (!File.Exists(filePath))
                {
                    await using var fs = File.OpenWrite(filePath);
                    await stream.CopyToAsync(fs, cancellationToken);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
            return entity;
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

            var dataStream = new MemoryStream();
            if (request.StorageType == BlobStorageType.Db)
            {
                var blob = await Task.FromResult(Uow.Query<BlobObject>().First(x => x.Id == request.BlobId));
                var dbFile = await Task.FromResult(Uow.Query<DbFile>().First(x => x.Md5 == blob.Md5));
                await dbFile.Data.DownloadAsync(dataStream, cancellation: cancellationToken);
                dataStream.Position = 0;
                return (blob, dataStream);
            }
            else if (request.StorageType == BlobStorageType.Cache)
            {
                var blob = await _redis.GetNamedAsync<BlobObject>(request.BlobId);
                var stream = _memCache.Get<Stream>(blob.Md5);
                return (blob, stream);
            }
            else if (request.StorageType == BlobStorageType.FileSystem)
            {
                var blob = await Task.FromResult(Uow.Query<BlobObject>().First(x => x.Id == request.BlobId));
                var filePath = GetFilePath(blob.Md5);
                await using var fs = File.OpenRead(filePath);
                await fs.CopyToAsync(dataStream, cancellationToken);
                dataStream.Position = 0;
                return (blob, dataStream);
            }
            throw new NotImplementedException();
        }
    }
}

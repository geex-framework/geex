using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;

using Geex.Common.BlobStorage.Core.Aggregates.BlobObjects;
using Geex.Common.BlobStorage.Api.Abstractions;
using Geex.Common.Requests.BlobStorage;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MimeKit;
using MongoDB.Entities;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

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
        public IUnitOfWork Uow { get; }

        public BlobObjectHandler(IUnitOfWork uow, IMemoryCache memCache, IRedisDatabase redis)
        {
            _memCache = memCache;
            _redis = redis;
            Uow = uow;
        }

        public async Task<IBlobObject> Handle(CreateBlobObjectRequest request, CancellationToken cancellationToken)
        {
            var stream = new MemoryStream();
            using (var readStream = request.File.OpenReadStream())
            {
                await readStream.CopyToAsync(stream, cancellationToken);
                stream.Position = 0;
            }
            request.Md5 ??= stream.Md5();
            var entity = new BlobObject(request.File.Name, request.Md5, request.StorageType, request.File.ContentType ?? MimeTypes.GetMimeType(request.File.Name), stream.Length);
            entity = Uow.Attach(entity);
            if (request.StorageType == BlobStorageType.Db)
            {
                var dbFile = Uow.Query<DbFile>().FirstOrDefault(x => x.Md5 == request.Md5);
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
            return entity;
        }

        public async Task Handle(DeleteBlobObjectRequest request, CancellationToken cancellationToken)
        {
            if (request.StorageType == BlobStorageType.Db)
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

        public Task<BlobObject> GetOrNullAsync(string id)
        {
            return Task.FromResult(Uow.Query<BlobObject>().First(x => x.Id == id));
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public async Task<(IBlobObject blob, Stream dataStream)> Handle(DownloadFileRequest request, CancellationToken cancellationToken)
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
            if (request.StorageType == BlobStorageType.Cache)
            {
                var blob = await _redis.GetNamedAsync<BlobObject>(request.BlobId);
                var stream = _memCache.Get<Stream>(blob.Md5);
                return (blob, stream);
            }
            throw new NotImplementedException();
        }
    }
}

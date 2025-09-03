using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Extensions.BlobStorage.Requests;
using Geex.Storage;
using Geex.Validation;

using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MimeKit;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Entities;

namespace Geex.Extensions.BlobStorage.Core.Entities
{
    /// <summary>
    /// Represents a blob object in the storage system
    /// </summary>
    public class BlobObject : Entity<BlobObject>, IBlobObject
    {
        private readonly Task? _streamToStorageTask = null;
        private static readonly ThreadLocal<byte[]> _bufferCache = new ThreadLocal<byte[]>(() => new byte[512 * 1024]);

        const long MaxCacheSize = 2048L * 1024 * 1024; // 2GB

        public BlobObject(CreateBlobObjectRequest request, IUnitOfWork uow = default)
        {
            uow?.Attach(this);
            var file = request.File;

            // 使用同步版本避免构造函数中的异步等待
            var existed = uow.Query<BlobObject>().Any(x => x.Md5 == request.Md5 && x.FileName == file.Name && x.MimeType == file.ContentType && x.StorageType == request.StorageType);
            if (existed)
            {
                throw new BusinessException("A blob object with the same filename and MD5 already exists for this storage type.");
            }

            var dataStream = file.OpenReadStream();
            var fileName = dataStream is FileStream fs ? Path.GetFileName(fs.Name) : file.Name;
            this.FileName = fileName;

            if (!string.IsNullOrEmpty(file.ContentType))
                this.MimeType = file.ContentType;
            else if (!string.IsNullOrEmpty(fileName))
                this.MimeType = GetContentType(fileName);
            this.Md5 = request.Md5;
            this.StorageType = request.StorageType;
            this.FileSize = file.Length ?? dataStream.Length;
            this.SaveAsync().GetAwaiter().GetResult();
            uow.Attach(this);
            this._streamToStorageTask = Task.Run(async () =>
            {
                // 保存到存储
                // 重新跟踪对象
                await this.StreamToStorage(dataStream);
                await dataStream.DisposeAsync();
            });
            uow.PreSaveChanges += async () => await this._streamToStorageTask;
            uow.PostSaveChanges += async () =>
            {
                if (!_streamToStorageTask.IsCompletedSuccessfully)
                {
                    using var scope = uow.ServiceProvider.CreateScope();
                    await scope.ServiceProvider.GetService<IUnitOfWork>().Query<BlobObject>().GetById(this.Id).DeleteAsync();
                }
            };
        }



        [Obsolete("for internal use only.", false)]
        internal BlobObject()
        {
        }

        public string? FileName { get; set; }
        public string? Md5 { get; set; }
        public string? Url =>
            $"{ServiceProvider.GetService<GeexCoreModuleOptions>().Host.Trim('/')}/{ServiceProvider.GetService<BlobStorageModuleOptions>().FileDownloadPath.Trim('/')}?fileId={this.Id}";
        public long FileSize { get; set; }
        public string? MimeType { get; set; }
        public BlobStorageType StorageType { get; set; }
        public DateTimeOffset? ExpireAt { get; set; }

        /// <summary>
        /// Calculates appropriate buffer size based on file size
        /// </summary>
        public static int GetBufferSize(long fileLength)
        {
            return fileLength switch
            {
                < 1048576L => 64 * 1024,        // 文件 < 1MB, 使用 64KB 缓冲区
                < 10485760L => 128 * 1024,      // 文件 < 10MB, 使用 128KB 缓冲区
                < 52428800L => 256 * 1024,      // 文件 < 50MB, 使用 256KB 缓冲区
                _ => 512 * 1024,                // 文件 >= 50MB, 使用 512KB 缓冲区
            };
        }

        /// <summary>
        /// Gets file path in the file system
        /// </summary>
        public string GetFilePath()
        {
            var options = this.DbContext.ServiceProvider.GetService<BlobStorageModuleOptions>();
            return Path.Combine(options.FileSystemStoragePath, this.Md5);
        }

        /// <summary>
        /// Processes a stream and calculates MD5 with improved memory efficiency
        /// </summary>
        public static async Task<string> ProcessStreamAsync(Stream inputStream, int bufferSize, MD5 md5Hasher, Func<ReadOnlyMemory<byte>, Task> writeAction, CancellationToken cancellationToken)
        {
            // Reuse buffer from thread-local cache if size matches
            var buffer = _bufferCache.Value.Length >= bufferSize ? _bufferCache.Value : new byte[bufferSize];
            int bytesRead;

            while ((bytesRead = await inputStream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken)) > 0)
            {
                var dataToProcess = buffer.AsMemory(0, bytesRead);
                md5Hasher.TransformBlock(buffer, 0, bytesRead, null, 0);
                if (writeAction != null)
                {
                    await writeAction(dataToProcess);
                }
            }

            md5Hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return BitConverter.ToString(md5Hasher.Hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Implements the IBlobObject.StreamToStorage method
        /// </summary>
        public async Task StreamToStorage(Stream dataStream, CancellationToken cancellationToken = default)
        {
            var bufferSize = GetBufferSize(this.FileSize);
            var logger = this.DbContext.ServiceProvider.GetService<ILogger<BlobObject>>();

            if (this.StorageType == BlobStorageType.Db)
            {
                if (!this.Md5.IsNullOrEmpty() && this.DbContext.Query<DbFile>().Any(x => x.Md5 == this.Md5))
                {
                    return;
                }

                var dbFile = this.DbContext.Attach(new DbFile(null));
                using var md5HasherDb = MD5.Create();

                await dbFile.Data.UploadAsync(dataStream, cancellationToken, md5HasherDb);
                var md5HashDb = BitConverter.ToString(md5HasherDb.Hash).Replace("-", "").ToLowerInvariant();

                this.Md5 = md5HashDb;
                dbFile.Md5 = md5HashDb;
            }
            else if (this.StorageType == BlobStorageType.FileSystem)
            {
                // 异步检查文件存在性
                if (!this.Md5.IsNullOrEmpty())
                {
                    var filePath = this.GetFilePath();
                    if (File.Exists(filePath))
                    {
                        return;
                    }
                }

                var tempFileName = ObjectId.GenerateNewId().ToString();
                var options = this.DbContext.ServiceProvider.GetService<BlobStorageModuleOptions>();
                var tempFilePath = Path.Combine(options.FileSystemStoragePath, tempFileName);

                using var md5HasherFs = MD5.Create();
                await using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write,
                    FileShare.None, bufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous);

                var md5HashFs = await ProcessStreamAsync(dataStream, bufferSize, md5HasherFs,
                    async (chunk) =>
                    {
                        await fileStream.WriteAsync(chunk, cancellationToken);
                    }, cancellationToken);

                await fileStream.FlushAsync(cancellationToken);
                await fileStream.DisposeAsync();

                var finalFilePath = Path.Combine(options.FileSystemStoragePath, md5HashFs);
                File.Move(tempFilePath, finalFilePath, true);

                this.Md5 = md5HashFs;
            }
            else if (this.StorageType == BlobStorageType.Cache)
            {
                // 设置过期时间
                this.ExpireAt = DateTimeOffset.Now.AddMinutes(5);

                // 检查大小限制
                if (this.FileSize > MaxCacheSize)
                    throw new InvalidOperationException($"缓存文件大小不得超过[{MaxCacheSize}]！");

                var tempFilePath2 = Path.GetFullPath(Path.Combine(Path.GetTempPath(), this.Id));
                await using var fileStream2 = new FileStream(tempFilePath2, FileMode.Create, FileAccess.Write,
                    FileShare.None, bufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous);
                using var md5HasherCache = MD5.Create();

                var md5HashCache = await ProcessStreamAsync(dataStream, bufferSize, md5HasherCache,
                    async (chunk) =>
                    {
                        await fileStream2.WriteAsync(chunk, cancellationToken);
                    }, cancellationToken);

                await fileStream2.FlushAsync(cancellationToken);
                this.Md5 = md5HashCache;

                // 设置自动删除计时器 - 使用更高效的清理机制
                var deleteTimer = new Timer(async state =>
                {
                    await TryDeleteCacheFileAsync(tempFilePath2, logger);
                }, null, TimeSpan.FromMinutes(5), Timeout.InfiniteTimeSpan);

                // 注册清理回调以避免内存泄漏
                cancellationToken.Register(() => deleteTimer?.Dispose());
            }
            else
                throw new NotImplementedException($"未实现的存储类型: {this.StorageType}");
        }

        /// <summary>
        /// Asynchronously attempts to delete cache file with retry logic
        /// </summary>
        private static async Task TryDeleteCacheFileAsync(string filePath, ILogger logger)
        {
            const int maxRetries = 3;
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        return;
                    }
                }
                catch (IOException ex) when (retry < maxRetries - 1)
                {
                    logger?.LogWarning(ex, "File is in use, retrying in 1 minute. Attempt {Retry}/{MaxRetries}", retry + 1, maxRetries);
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to delete cache file: {FilePath}", filePath);
                    break;
                }
            }
        }

        /// <summary>
        /// Gets file content type from the provided file
        /// </summary>
        private static string GetContentType(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                return MimeTypes.GetMimeType(fileName);
            }

            return "application/octet-stream";
        }

        /// <summary>
        /// Overrides the base DeleteAsync method to handle storage cleanup
        /// </summary>
        /// <param name="cancellation"></param>
        public override async Task<long> DeleteAsync(CancellationToken cancellation = default)
        {
            // 先执行存储清理
            await this.TryDeleteStorageData(cancellation);

            // 然后调用基类方法删除实体
            return await base.DeleteAsync(cancellation);
        }

        /// <summary>
        /// Deletes the storage data associated with this blob object
        /// </summary>
        private async Task TryDeleteStorageData(CancellationToken cancellationToken = default)
        {
            if (this.StorageType == BlobStorageType.FileSystem && !string.IsNullOrEmpty(this.Md5))
            {
                // 检查是否有其他对象引用相同的MD5
                var duplicateCount = await this.DbContext.CountAsync<BlobObject>(x => x.Md5 == this.Md5, cancellationToken);
                if (duplicateCount <= 1)
                {
                    var filePath = this.GetFilePath();
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
            else if (this.StorageType == BlobStorageType.Db && !string.IsNullOrEmpty(this.Md5))
            {
                // 检查是否有其他对象引用相同的MD5
                var duplicateCount = await this.DbContext.CountAsync<BlobObject>(x => x.Md5 == this.Md5, cancellationToken);
                if (duplicateCount <= 1)
                {
                    var dbFile = this.DbContext.Query<DbFile>().FirstOrDefault(x => x.Md5 == this.Md5);
                    if (dbFile != null)
                    {
                        await dbFile.Data.ClearAsync(cancellationToken);
                        await dbFile.DeleteAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Streams the file from storage based on storage type
        /// </summary>
        public async Task<Stream> StreamFromStorage(CancellationToken cancellationToken = default)
        {
            if (_streamToStorageTask != default)
            {
                await _streamToStorageTask;
            }
            Stream dataStream;

            if (this.StorageType == BlobStorageType.Db)
            {
                if (this.Md5.IsNullOrEmpty())
                {
                    await this.HandleInvalidBlobAsync(cancellationToken);
                }

                // 使用异步查询
                var dbFile = this.DbContext.Query<DbFile>().FirstOrDefault(x => x.Md5 == this.Md5);
                if (dbFile == default)
                {
                    await this.HandleInvalidBlobAsync(cancellationToken);
                }

                dataStream = await dbFile.Data.DownloadAsStreamAsync(4, cancellationToken);
                return dataStream;
            }
            else if (this.StorageType == BlobStorageType.Cache)
            {
                var tempFilePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), this.Id));
                if (!File.Exists(tempFilePath))
                {
                    throw new FileNotFoundException("Cached files only available within 5 minutes.");
                }

                var cachedStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                    GetBufferSize(this.FileSize), FileOptions.Asynchronous);
                return cachedStream;
            }
            else if (this.StorageType == BlobStorageType.FileSystem)
            {
                if (this.Md5.IsNullOrEmpty())
                {
                    await this.HandleInvalidBlobAsync(cancellationToken);
                }

                var filePath = this.GetFilePath();
                if (!File.Exists(filePath))
                {
                    await this.HandleInvalidBlobAsync(cancellationToken);
                }

                var bufferSize = GetBufferSize(this.FileSize);
                dataStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize,
                    FileOptions.SequentialScan | FileOptions.Asynchronous);
                return dataStream;
            }

            throw new NotImplementedException($"未实现的存储类型: {this.StorageType}");
        }

        /// <summary>
        /// Handles invalid blob by deleting it and throwing exception
        /// </summary>
        private async Task HandleInvalidBlobAsync(CancellationToken cancellationToken)
        {
            throw new BusinessException("File is corrupted, please try upload again.");
        }

        public override async Task<ValidationResult> Validate(CancellationToken cancellation = default)
        {
            return ValidationResult.Success;
        }

        public class BlobObjectBsonConfig : BsonConfig<BlobObject>
        {
            protected override void Map(BsonClassMap<BlobObject> map, BsonIndexConfig<BlobObject> indexConfig)
            {
                map.Inherit<IBlobObject>();
                map.AutoMap();
                indexConfig.MapEntityDefaultIndex();
                indexConfig.MapIndex(x => x.Hashed(y => y.Md5), options =>
                {
                    options.Background = true;
                });
                indexConfig.MapIndex(x => x.Ascending(y => y.ExpireAt), options =>
                {
                    options.ExpireAfter = TimeSpan.FromSeconds(0);
                });
            }
        }


        public class BlobObjectGqlConfig : GqlConfig.Object<BlobObject>
        {
            /// <inheritdoc />
            protected override void Configure(IObjectTypeDescriptor<BlobObject> descriptor)
            {
                descriptor.Implements<InterfaceType<IBlobObject>>();
                descriptor.BindFieldsImplicitly();
                descriptor.ConfigEntity();
            }
        }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstractions;
using Geex.Common.BlobStorage.Api.Abstractions;
using Geex.Common.Requests.BlobStorage;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Entities;
using MediatR;
using MimeKit;

namespace Geex.Common.BlobStorage.Core.Aggregates.BlobObjects
{
    /// <summary>
    /// Represents a blob object in the storage system
    /// </summary>
    public class BlobObject : Abstraction.Storage.Entity<BlobObject>, IBlobObject
    {
        const long MaxCacheSize = 2048L * 1024 * 1024; // 2GB

        public BlobObject(string fileName, string md5, BlobStorageType storageType, string mimeType, long fileSize, IUnitOfWork uow = default)
        {
            this.FileName = fileName;
            this.Md5 = md5;
            this.MimeType = mimeType;
            this.FileSize = fileSize;
            this.StorageType = storageType;
            uow?.Attach(this);
        }

        public BlobObject(CreateBlobObjectRequest request, IUnitOfWork uow = default)
        {
            var existed = Uow.Query<BlobObject>().FirstOrDefault(x => x.Md5 == request.Md5);
            if (existed != default)
            {
                if (existed.FileName == request.File.Name && existed.MimeType == request.File.ContentType && existed.StorageType == request.StorageType)
                {
                    throw new BusinessException("A blob object with the same filename and MD5 already exists for this storage type.");
                }
            }
            var file = request.File;
            // 打开文件流
            using var readStream = file.OpenReadStream();

            // 获取文件信息
            this.FileSize = file.Length ?? readStream.Length;
            var fileName = readStream is FileStream fs ? Path.GetFileName(fs.Name) : file.Name;
            this.FileName = fileName;
            string fileContentType;

            if (!string.IsNullOrEmpty(file.ContentType))
                fileContentType = file.ContentType;
            else if (!string.IsNullOrEmpty(fileName))
                fileContentType = GetContentType(fileName);
            // 保存到存储
            this.StreamToStorage(readStream).ConfigureAwait(true).GetAwaiter().GetResult();
            uow?.Attach(this);
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
        /// Processes a stream and calculates MD5
        /// </summary>
        public static async Task<string> ProcessStreamAsync(Stream inputStream, int bufferSize, MD5 md5Hasher, Func<byte[], int, Task> writeAction, CancellationToken cancellationToken)
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
        /// <summary>
        /// Implements the IBlobObject.StreamToStorage method
        /// </summary>
        public async Task StreamToStorage(Stream dataStream, CancellationToken cancellationToken = default)
        {
            var bufferSize = GetBufferSize(this.FileSize);
            var logger = this.DbContext.ServiceProvider.GetService<ILogger<BlobObject>>();

            if (this.StorageType == BlobStorageType.Db)
            {
                // 检查是否已存在相同MD5的数据
                if (!this.Md5.IsNullOrEmpty() && this.DbContext.Query<DbFile>().Any(x => x.Md5 == this.Md5))
                {
                    return;
                }

                var dbFile = this.DbContext.Attach(new DbFile(null));

                // 使用MD5哈希算法
                using var md5HasherDb = MD5.Create();

                // 上传数据并计算MD5
                await dbFile.Data.UploadAsync(dataStream, cancellationToken, md5HasherDb);
                var md5HashDb = BitConverter.ToString(md5HasherDb.Hash).Replace("-", "").ToLowerInvariant();

                this.Md5 = md5HashDb;
                dbFile.Md5 = md5HashDb;
            }

            else if (this.StorageType == BlobStorageType.FileSystem)
            {
                // 检查是否已存在相同MD5的文件
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
                    FileShare.None, bufferSize, FileOptions.SequentialScan);

                var md5HashFs = await ProcessStreamAsync(dataStream, bufferSize, md5HasherFs,
                    async (chunk, length) =>
                    {
                        await fileStream.WriteAsync(chunk.AsMemory(0, length), cancellationToken);
                    }, cancellationToken);

                // 关闭文件流并重命名临时文件
                await fileStream.DisposeAsync().ConfigureAwait(false);
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

                await using var fileStream2 = File.Create(tempFilePath2);
                using var md5HasherCache = MD5.Create();

                var md5HashCache = await ProcessStreamAsync(dataStream, bufferSize, md5HasherCache,
                    async (chunk, length) =>
                    {
                        await fileStream2.WriteAsync(chunk.AsMemory(0, length), cancellationToken);
                    }, cancellationToken);

                this.Md5 = md5HashCache;

                // 设置自动删除计时器
                Timer deleteTimer = default;
                deleteTimer = new Timer(state =>
                {
                    try
                    {
                        if (File.Exists(tempFilePath2))
                        {
                            File.Delete(tempFilePath2);
                            deleteTimer?.Dispose(); // 删除成功后清理定时器
                        }
                    }
                    catch (IOException) // 如果文件正在被使用，删除会失败，捕获异常
                    {
                        logger.LogWarning("File is in use, retrying in 1 minute.");
                        deleteTimer?.Change(TimeSpan.FromMinutes(1), Timeout.InfiniteTimeSpan); // 重试1分钟后删除
                    }
                }, null, TimeSpan.FromMinutes(5), Timeout.InfiniteTimeSpan);
            }

            else
                throw new NotImplementedException($"未实现的存储类型: {this.StorageType}");

            return;
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
        public override async Task<long> DeleteAsync()
        {
            // 先执行存储清理
            await this.TryDeleteStorageData();

            // 然后调用基类方法删除实体
            return await base.DeleteAsync();
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
                    var dbFile = await Task.FromResult(this.DbContext.Query<DbFile>().Single(x => x.Md5 == this.Md5));
                    await dbFile.Data.ClearAsync(cancellationToken);
                    await dbFile.DeleteAsync();
                }
            }
        }
        /// <summary>
        /// Streams the file from storage based on storage type
        /// </summary>
        public async Task<Stream> StreamFromStorage(CancellationToken cancellationToken = default)
        {
            Stream dataStream;

            if (this.StorageType == BlobStorageType.Db)
            {
                if (this.Md5.IsNullOrEmpty())
                {
                    await this.HandleInvalidBlobAsync(cancellationToken);
                }

                var dbFile = await Task.FromResult(this.DbContext.Query<DbFile>().FirstOrDefault(x => x.Md5 == this.Md5));
                if (dbFile == default)
                {
                    await this.HandleInvalidBlobAsync(cancellationToken);
                }

                // 直接使用数据库提供的流
                dataStream = await dbFile.Data.DownloadAsStreamAsync(4, cancellationToken);
                return dataStream;
            }
            else if (this.StorageType == BlobStorageType.Cache)
            {
                // 从缓存中获取Stream
                var tempFilePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), this.Id));
                if (!File.Exists(tempFilePath))
                {
                    throw new FileNotFoundException("Cached files only available within 5 minutes.");
                }

                var cachedStream = File.OpenRead(tempFilePath);
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

                // 直接返回文件的FileStream
                var bufferSize = GetBufferSize(this.FileSize);
                dataStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
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

        public override async Task<ValidationResult> Validate(IServiceProvider sp, CancellationToken cancellation = default)
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using Geex.MongoDB.Entities.Core;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

[assembly: InternalsVisibleTo("MongoDB.Entities.Tests")]
namespace MongoDB.Entities
{
    /// <summary>
    /// Inherit this base class in order to create your own File Entities
    /// </summary>
    public abstract class FileEntity : EntityBase<FileEntity>
    {
        private DataStreamer streamer;

        /// <summary>
        /// The total amount of data in bytes that has been uploaded so far
        /// </summary>
        [BsonElement]
        public long FileSize { get; internal set; }

        /// <summary>
        /// The number of chunks that have been created so far
        /// </summary>
        [BsonElement]
        public int ChunkCount { get; internal set; }

        /// <summary>
        /// Returns true only when all the chunks have been stored successfully in mongodb
        /// </summary>
        [BsonElement]
        public bool UploadSuccessful { get; internal set; }

        /// <summary>
        /// Access the DataStreamer class for uploading and downloading data
        /// </summary>
        public DataStreamer Data
        {
            get
            {
                return streamer ?? (streamer = new DataStreamer(this));
            }
        }
    }

    [Name("[BINARY_CHUNKS]")]
    internal class FileChunk : EntityBase<FileChunk>
    {
        public ObjectId FileId { get; set; }

        public byte[] Data { get; set; }
    }

    /// <summary>
    /// Provides the interface for uploading and downloading data chunks for file entities.
    /// </summary>
    public class DataStreamer
    {
        // 获取缓冲区大小
        public static int GetBufferSize(long fileLength)
        {
            return fileLength switch
            {
                < 1048576L => 64 * 1024,        // 文件 < 1MB, 使用 64 缓冲区
                < 10485760L => 128 * 1024,        // 文件 < 10MB, 使用 128 缓冲区
                < 52428800L => 256 * 1024,      // 文件 < 50MB, 使用 256 缓冲区
                _ => 512 * 1024,               // 文件 >= 50MB, 使用 512 缓冲区
            };
        }
        private static readonly HashSet<string> indexedDBs = new HashSet<string>();

        private readonly FileEntity parent;
        private readonly Type parentType;
        private readonly IMongoDatabase db;
        private readonly IMongoCollection<FileChunk> chunkCollection;
        private FileChunk doc;
        private int chunkSize = 15728640;
        private int bufferSize = 65536;
        private int readCount;
        private MD5? md5Hasher;

        public DataStreamer(FileEntity parent)
        {
            this.parent = parent;
            parentType = parent.GetType();

            db = TypeMap.GetDatabase(parentType);

            chunkCollection = db.GetCollection<FileChunk>(DB.CollectionName<FileChunk>());

            var dbName = db.DatabaseNamespace.DatabaseName;

            if (!indexedDBs.Contains(dbName))
            {
                indexedDBs.Add(dbName);

                _ = chunkCollection.Indexes.CreateOneAsync(
                    new CreateIndexModel<FileChunk>(
                        Builders<FileChunk>.IndexKeys.Ascending(c => c.FileId),
                        new CreateIndexOptions { Background = true, Name = $"{nameof(FileChunk.FileId)}(Asc)" }));
            }
        }

        /// <summary>
        /// Download binary data for this file entity from mongodb in chunks into a given stream with a timeout period.
        /// </summary>
        /// <param name="stream">The output stream to write the data</param>
        /// <param name="timeOutSeconds">The maximum number of seconds allowed for the operation to complete</param>
        /// <param name="batchSize"></param>
        /// <param name="session"></param>
        public Task DownloadWithTimeoutAsync(Stream stream, int timeOutSeconds, int batchSize = 1)
        {
            return DownloadAsync(stream, batchSize, new CancellationTokenSource(timeOutSeconds * 1000).Token);
        }


        public async Task<Stream> DownloadAsStreamAsync(int batchSize = 1, CancellationToken cancellation = default)
        {
            parent.ThrowIfUnsaved();
            if (!parent.UploadSuccessful)
                throw new InvalidOperationException("Data for this file hasn't been uploaded successfully (yet)!");

            var filter = Builders<FileChunk>.Filter.Eq(c => c.FileId, parent.Id);
            var options = new FindOptions<FileChunk, byte[]>
            {
                BatchSize = batchSize,
                Sort = Builders<FileChunk>.Sort.Ascending(c => c.Id),
                Projection = Builders<FileChunk>.Projection.Expression(c => c.Data)
            };

            // Initiate the FindAsync operation
            IAsyncCursor<byte[]> cursor = ((IEntityBase)parent).DbContext?.session == null
                ? await chunkCollection.FindAsync(filter, options, cancellation).ConfigureAwait(false)
                : await chunkCollection.FindAsync(((IEntityBase)parent).DbContext?.Session, filter, options, cancellation).ConfigureAwait(false);

            // Return the custom AsyncFileStream
            return new AsyncCursorStream(cursor, parent.FileSize, (int)Math.Min(parent.FileSize, chunkSize));
        }


        /// <summary>
        /// Download binary data for this file entity from mongodb in chunks into a given stream.
        /// </summary>
        /// <param name="stream">The output stream to write the data</param>
        /// <param name="batchSize">The number of chunks you want returned at once</param>
        /// <param name="cancellation">An optional cancellation token.</param>

        public async Task DownloadAsync(Stream stream, int batchSize = 1, CancellationToken cancellation = default)
        {
            parent.ThrowIfUnsaved();
            if (!parent.UploadSuccessful) throw new InvalidOperationException("Data for this file hasn't been uploaded successfully (yet)!");
            if (!stream.CanWrite) throw new NotSupportedException("The supplied stream is not writable!");

            var filter = Builders<FileChunk>.Filter.Eq(c => c.FileId, parent.Id);
            var options = new FindOptions<FileChunk, byte[]>
            {
                BatchSize = batchSize,
                Sort = Builders<FileChunk>.Sort.Ascending(c => c.Id),
                Projection = Builders<FileChunk>.Projection.Expression(c => c.Data)
            };

            var findTask =
                ((IEntityBase)parent).DbContext?.session == null
                ? chunkCollection.FindAsync(filter, options, cancellation)
                : chunkCollection.FindAsync(((IEntityBase)parent).DbContext?.Session, filter, options, cancellation);

            using (var cursor = await findTask.ConfigureAwait(false))
            {
                var hasChunks = false;

                while (await cursor.MoveNextAsync(cancellation).ConfigureAwait(false))
                {
                    foreach (var chunk in cursor.Current)
                    {
                        await stream.WriteAsync(chunk, 0, chunk.Length, cancellation).ConfigureAwait(false);
                        hasChunks = true;
                    }
                }

                if (!hasChunks) throw new InvalidOperationException($"No data was found for file entity with Id: {parent.Id}");
            }
        }

        /// <summary>
        /// Clear chunks
        /// </summary>
        /// <param name="stream">The output stream to write the data</param>
        /// <param name="batchSize">The number of chunks you want returned at once</param>
        /// <param name="cancellation">An optional cancellation token.</param>

        public async Task<DeleteResult> ClearAsync(CancellationToken cancellation = default)
        {
            parent.ThrowIfUnsaved();
            if (!parent.UploadSuccessful) throw new InvalidOperationException("Data for this file hasn't been uploaded successfully (yet)!");

            var filter = Builders<FileChunk>.Filter.Eq(c => c.FileId, parent.Id);

            return ((IEntityBase)parent).DbContext?.session == null
                ? await chunkCollection.DeleteManyAsync(filter, cancellation)
                : await chunkCollection.DeleteManyAsync(((IEntityBase)parent).DbContext?.Session, filter, null, cancellation);
        }

        /// <summary>
        /// Upload binary data for this file entity into mongodb in chunks from a given stream with a timeout period.
        /// </summary>
        /// <param name="stream">The input stream to read the data from</param>
        /// <param name="timeOutSeconds">The maximum number of seconds allowed for the operation to complete</param>

        public Task UploadWithTimeoutAsync(Stream stream, int timeOutSeconds)
        {
            return UploadAsync(stream, new CancellationTokenSource(timeOutSeconds * 1000).Token);
        }

        /// <summary>
        /// Upload binary data for this file entity into mongodb in chunks from a given stream.
        /// <para>TIP: Make sure to save the entity before calling this method.</para>
        /// </summary>
        /// <param name="stream">The input stream to read the data from</param>
        /// <param name="cancellation">An optional cancellation token.</param>
        /// <param name="md5Hasher"></param>
        public async Task UploadAsync(Stream stream, CancellationToken cancellation = default, MD5 md5Hasher = null)
        {
            this.md5Hasher = md5Hasher;
            parent.ThrowIfUnsaved();
            if (!stream.CanRead) throw new NotSupportedException("The supplied stream is not readable!");
            await CleanUpAsync(((IEntityBase)parent).DbContext).ConfigureAwait(false);

            doc = new FileChunk { FileId = parent.Id, CreatedOn = DateTimeOffset.Now };
            chunkSize = (int)Math.Min(chunkSize, stream.Length);
            var dataChunk = new Memory<byte>(new byte[chunkSize]);
            readCount = 0;
            var dbContext = ((IEntityBase)parent).DbContext;
            try
            {
                if (stream.CanSeek && stream.Position > 0) stream.Position = 0;

                while ((readCount = stream.Read(dataChunk.Span)) > 0)
                {
                    var readBytes = dataChunk[..readCount].ToArray();
                    readBytes.CopyTo(dataChunk);
                    md5Hasher?.TransformBlock(readBytes, 0, readBytes.Length, null, 0);
                    doc.Id = doc.GenerateNewId();
                    doc.Data = readBytes;
                    parent.ChunkCount++;
                    await (dbContext?.Session == null
                        ? chunkCollection.InsertOneAsync(doc, null, cancellation)
                        : chunkCollection.InsertOneAsync(dbContext?.Session, doc, null, cancellation));
                    parent.FileSize += readCount;
                }

                if (parent.FileSize > 0)
                {
                    md5Hasher?.TransformFinalBlock([], 0, 0);
                    parent.UploadSuccessful = true;
                }
                else
                {
                    throw new InvalidOperationException("The supplied stream had no data to read (probably closed)");
                }
            }
            catch (Exception)
            {
                await CleanUpAsync(dbContext).ConfigureAwait(false);
                throw;
            }
            finally
            {
                await UpdateMetaDataAsync(dbContext).ConfigureAwait(false);
                doc = null;
            }
        }

        private Task CleanUpAsync(DbContext dbContext)
        {
            parent.FileSize = 0;
            parent.ChunkCount = 0;
            parent.UploadSuccessful = false;
            return dbContext?.Session == null
                   ? chunkCollection.DeleteManyAsync(c => c.FileId == parent.Id)
                   : chunkCollection.DeleteManyAsync(dbContext?.Session, c => c.FileId == parent.Id);
        }

        private Task UpdateMetaDataAsync(DbContext dbContext)
        {
            var collection = DB.Collection<FileEntity>();
            var filter = Builders<FileEntity>.Filter.Eq(e => e.Id, parent.Id);
            var update = Builders<FileEntity>.Update
                            .Set(e => e.FileSize, parent.FileSize)
                            .Set(e => e.ChunkCount, parent.ChunkCount)
                            .Set(e => e.UploadSuccessful, parent.UploadSuccessful);

            return dbContext?.Session == null
                   ? collection.UpdateOneAsync(filter, update)
                   : collection.UpdateOneAsync(dbContext?.Session, filter, update);
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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
        [ObjectId]
        public string FileId { get; set; }

        public byte[] Data { get; set; }
    }

    /// <summary>
    /// Provides the interface for uploading and downloading data chunks for file entities.
    /// </summary>
    public class DataStreamer
    {
        private static readonly HashSet<string> indexedDBs = new HashSet<string>();

        private readonly FileEntity parent;
        private readonly Type parentType;
        private readonly IMongoDatabase db;
        private readonly IMongoCollection<FileChunk> chunkCollection;
        private FileChunk doc;
        private int chunkSize, readCount;
        private byte[] buffer;
        private List<byte> dataChunk;

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
        /// <param name="chunkSizeKB">The 'average' size of one chunk in KiloBytes</param>

        public Task UploadWithTimeoutAsync(Stream stream, int timeOutSeconds, int chunkSizeKB = 256)
        {
            return UploadAsync(stream, chunkSizeKB, new CancellationTokenSource(timeOutSeconds * 1000).Token);
        }

        /// <summary>
        /// Upload binary data for this file entity into mongodb in chunks from a given stream.
        /// <para>TIP: Make sure to save the entity before calling this method.</para>
        /// </summary>
        /// <param name="stream">The input stream to read the data from</param>
        /// <param name="chunkSizeKB">The 'average' size of one chunk in KiloBytes</param>
        /// <param name="cancellation">An optional cancellation token.</param>

        public async Task UploadAsync(Stream stream, int chunkSizeKB = 256, CancellationToken cancellation = default)
        {
            parent.ThrowIfUnsaved();
            if (chunkSizeKB < 128 || chunkSizeKB > 4096) throw new ArgumentException("Please specify a chunk size from 128KB to 4096KB");
            if (!stream.CanRead) throw new NotSupportedException("The supplied stream is not readable!");
            await CleanUpAsync(((IEntityBase)parent).DbContext).ConfigureAwait(false);

            doc = new FileChunk { FileId = parent.Id, CreatedOn = DateTimeOffset.Now };
            chunkSize = chunkSizeKB * 1024;
            dataChunk = new List<byte>(chunkSize);
            buffer = new byte[64 * 1024]; // 64kb read buffer
            readCount = 0;

            try
            {
                if (stream.CanSeek && stream.Position > 0) stream.Position = 0;

                while ((readCount = await stream.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false)) > 0)
                {
                    await FlushToDBAsync(((IEntityBase)parent).DbContext, isLastChunk: false, cancellation).ConfigureAwait(false);
                }

                if (parent.FileSize > 0)
                {
                    await FlushToDBAsync(((IEntityBase)parent).DbContext, isLastChunk: true, cancellation).ConfigureAwait(false);
                    parent.UploadSuccessful = true;
                }
                else
                {
                    throw new InvalidOperationException("The supplied stream had no data to read (probably closed)");
                }
            }
            catch (Exception)
            {
                await CleanUpAsync(((IEntityBase)parent).DbContext).ConfigureAwait(false);
                throw;
            }
            finally
            {
                await UpdateMetaDataAsync(((IEntityBase)parent).DbContext).ConfigureAwait(false);
                doc = null;
                buffer = null;
                dataChunk = null;
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

        private Task FlushToDBAsync(DbContext dbContext, bool isLastChunk = false, CancellationToken cancellation = default)
        {
            if (!isLastChunk)
            {
                dataChunk.AddRange(new ArraySegment<byte>(buffer, 0, readCount));
                parent.FileSize += readCount;
            }

            if (dataChunk.Count >= chunkSize || isLastChunk)
            {
                doc.Id = doc.GenerateNewId().ToString();
                doc.Data = dataChunk.ToArray();
                dataChunk.Clear();
                parent.ChunkCount++;
                return dbContext?.Session == null
                       ? chunkCollection.InsertOneAsync(doc, null, cancellation)
                       : chunkCollection.InsertOneAsync(dbContext?.Session, doc, null, cancellation);
            }

            return Task.CompletedTask;
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

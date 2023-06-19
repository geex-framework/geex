using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Driver;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Entities.Utilities
{
    public static class AsyncCursorExtension
    {
        public static IEnumerable<TDocument> Current<TDocument>(this IAsyncCursor<TDocument> cursor, DbContext contextOverride) where TDocument : IEntityBase
        {
            return cursor.Current.Select(x =>
            {
                x.DbContext = contextOverride;
                return x;
            });
        }
        /// <summary>Returns the first document of a cursor.</summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="cursor">The cursor.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The first document.</returns>
        public static TDocument First<TDocument>(
          this IAsyncCursor<TDocument> cursor,
          DbContext contextOverride,
          CancellationToken cancellationToken = default(CancellationToken)) where TDocument : IEntityBase
        {
            using (cursor)
            {
                var document = GetFirstBatch<TDocument>(cursor, cancellationToken).First<TDocument>();
                document.DbContext = contextOverride;
                return document;
            }
        }

        /// <summary>Returns the first document of a cursor.</summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="cursor">The cursor.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the first document.</returns>
        public static async Task<TDocument> FirstAsync<TDocument>(
          this IAsyncCursor<TDocument> cursor,
          DbContext contextOverride,
          CancellationToken cancellationToken = default(CancellationToken)) where TDocument : IEntityBase
        {
            TDocument document;
            using (cursor)
            {
                document = (await GetFirstBatchAsync<TDocument>(cursor, cancellationToken).ConfigureAwait(false))
                    .First<TDocument>();
                document.DbContext = contextOverride;
            }
            return document;
        }

        /// <summary>
        /// Returns the first document of a cursor, or a default value if the cursor contains no documents.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="cursor">The cursor.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The first document of the cursor, or a default value if the cursor contains no documents.</returns>
        public static TDocument FirstOrDefault<TDocument>(
          this IAsyncCursor<TDocument> cursor,
          DbContext contextOverride,
          CancellationToken cancellationToken = default(CancellationToken)) where TDocument : IEntityBase
        {
            using (cursor)
            {
                var document = GetFirstBatch<TDocument>(cursor, cancellationToken).FirstOrDefault<TDocument>();
                if (document != null)
                {
                    document.DbContext = contextOverride;
                }
                return document;
            }
        }

        /// <summary>
        /// Returns the first document of the cursor, or a default value if the cursor contains no documents.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="cursor">The cursor.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result is the first document of the cursor, or a default value if the cursor contains no documents.</returns>
        public static async Task<TDocument> FirstOrDefaultAsync<TDocument>(
          this IAsyncCursor<TDocument> cursor,
          DbContext contextOverride,
          CancellationToken cancellationToken = default(CancellationToken)) where TDocument : IEntityBase
        {
            TDocument document;
            using (cursor)
                document = (await GetFirstBatchAsync<TDocument>(cursor, cancellationToken).ConfigureAwait(false)).FirstOrDefault<TDocument>();
            if (document != null)
            {
                document.DbContext = contextOverride;
            }
            return document;
        }

        /// <summary>
        /// Returns the only document of a cursor. This method throws an exception if the cursor does not contain exactly one document.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="cursor">The cursor.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The only document of a cursor.</returns>
        public static TDocument Single<TDocument>(
          this IAsyncCursor<TDocument> cursor,
          DbContext contextOverride,
          CancellationToken cancellationToken = default(CancellationToken)) where TDocument : IEntityBase
        {
            using (cursor)
            {
                var document = GetFirstBatch<TDocument>(cursor, cancellationToken).Single<TDocument>();
                document.DbContext = contextOverride;
                return document;
            }
        }

        /// <summary>
        /// Returns the only document of a cursor. This method throws an exception if the cursor does not contain exactly one document.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="cursor">The cursor.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the only document of a cursor.</returns>
        public static async Task<TDocument> SingleAsync<TDocument>(
          this IAsyncCursor<TDocument> cursor,
          CancellationToken cancellationToken = default(CancellationToken))
        {
            TDocument document;
            using (cursor)
                document = (await GetFirstBatchAsync<TDocument>(cursor, cancellationToken).ConfigureAwait(false)).Single<TDocument>();
            return document;
        }

        /// <summary>
        /// Returns the only document of a cursor, or a default value if the cursor contains no documents.
        /// This method throws an exception if the cursor contains more than one document.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="cursor">The cursor.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The only document of a cursor, or a default value if the cursor contains no documents.</returns>
        public static TDocument SingleOrDefault<TDocument>(
          this IAsyncCursor<TDocument> cursor,
          CancellationToken cancellationToken = default(CancellationToken))
        {
            using (cursor)
                return GetFirstBatch<TDocument>(cursor, cancellationToken).SingleOrDefault<TDocument>();
        }

        /// <summary>
        /// Returns the only document of a cursor, or a default value if the cursor contains no documents.
        /// This method throws an exception if the cursor contains more than one document.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="cursor">The cursor.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is the only document of a cursor, or a default value if the cursor contains no documents.</returns>
        public static async Task<TDocument> SingleOrDefaultAsync<TDocument>(
          this IAsyncCursor<TDocument> cursor,
          CancellationToken cancellationToken = default(CancellationToken))
        {
            TDocument document;
            using (cursor)
                document = (await GetFirstBatchAsync<TDocument>(cursor, cancellationToken).ConfigureAwait(false)).SingleOrDefault<TDocument>();
            return document;
        }

        /// <summary>
        /// Wraps a cursor in an IEnumerable that can be enumerated one time.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="cursor">The cursor.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An IEnumerable</returns>
        public static IEnumerable<TDocument> ToEnumerable<TDocument>(
          this IAsyncCursor<TDocument> cursor,
          DbContext contextOverride,
          CancellationToken cancellationToken = default(CancellationToken)) where TDocument : IEntityBase
        {
            return cursor.ToEnumerable().Select(x =>
            {
                x.DbContext = contextOverride;
                return x;
            });
        }

        /// <summary>
        /// Returns a list containing all the documents returned by a cursor.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of documents.</returns>
        public static List<TDocument> ToList<TDocument>(
          this IAsyncCursor<TDocument> source,
          CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull<IAsyncCursor<TDocument>>(source, nameof(source));
            List<TDocument> documentList = new List<TDocument>();
            using (source)
            {
                while (source.MoveNext(cancellationToken))
                {
                    documentList.AddRange(source.Current);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            return documentList;
        }

        /// <summary>
        /// Returns a list containing all the documents returned by a cursor.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose value is the list of documents.</returns>
        public static async Task<List<TDocument>> ToListAsync<TDocument>(
          this IAsyncCursor<TDocument> source,
          CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull<IAsyncCursor<TDocument>>(source, nameof(source));
            List<TDocument> list = new List<TDocument>();
            using (source)
            {
                while (true)
                {
                    if (await source.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    {
                        list.AddRange(source.Current);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    else
                        break;
                }
            }
            List<TDocument> documentList = list;
            list = (List<TDocument>)null;
            return documentList;
        }

        private static IEnumerable<TDocument> GetFirstBatch<TDocument>(
      IAsyncCursor<TDocument> cursor,
      CancellationToken cancellationToken)
        {
            return cursor.MoveNext(cancellationToken) ? cursor.Current : Enumerable.Empty<TDocument>();
        }

        private static async Task<IEnumerable<TDocument>> GetFirstBatchAsync<TDocument>(
          IAsyncCursor<TDocument> cursor,
          CancellationToken cancellationToken)
        {
            return !await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false) ? Enumerable.Empty<TDocument>() : cursor.Current;
        }
    }
}

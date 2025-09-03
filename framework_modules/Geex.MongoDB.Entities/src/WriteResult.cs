using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MongoDB.Driver;

namespace MongoDB.Entities
{
    public interface IBulkWriteResult
    {
        bool IsAcknowledged { get; }
        long MatchedCount { get; }
        IReadOnlyList<BulkWriteUpsert>? Upserts { get; }
        long DeletedCount { get; }
        long InsertedCount { get; }
        long ModifiedCount { get; }
    }

    public struct BulkWriteResult<T> : IBulkWriteResult where T : IEntityBase
    {
        public BulkWriteResult(MongoDB.Driver.BulkWriteResult<T>.Acknowledged baseResult)
        {
            IsAcknowledged = baseResult.IsAcknowledged;
            MatchedCount = baseResult.MatchedCount;
            Upserts = baseResult.Upserts;
            DeletedCount = baseResult.DeletedCount;
            InsertedCount = baseResult.InsertedCount;
            ModifiedCount = baseResult.ModifiedCount;
        }

        public BulkWriteResult(MongoDB.Driver.BulkWriteResult<T>.Unacknowledged _)
        {
            IsAcknowledged = false;
            MatchedCount = 0;
            Upserts = [];
            DeletedCount = 0;
            InsertedCount = 0;
            ModifiedCount = 0;
        }

        public bool IsAcknowledged { get; private set; }

        public long MatchedCount { get; private set; }

        public IReadOnlyList<BulkWriteUpsert> Upserts { get; private set; }

        public long DeletedCount { get; private set; }

        public long InsertedCount { get; private set; }

        public long ModifiedCount { get; private set; }

        public static implicit operator BulkWriteResult<T>(MongoDB.Driver.BulkWriteResult baseResult)
        {
            if (baseResult.IsAcknowledged)
            {
                return new BulkWriteResult<T>(baseResult as MongoDB.Driver.BulkWriteResult<T>.Acknowledged);
            }
            else
            {
                return new BulkWriteResult<T>(baseResult as MongoDB.Driver.BulkWriteResult<T>.Unacknowledged);
            }
        }

        public static implicit operator BulkWriteResult<T>(MongoDB.Driver.BulkWriteResult<T>.Acknowledged baseResult)
        {
            return new BulkWriteResult<T>(baseResult);
        }

        public static implicit operator BulkWriteResult<T>(MongoDB.Driver.BulkWriteResult<T>.Unacknowledged baseResult)
        {
            return new BulkWriteResult<T>(baseResult);
        }
    }

    public struct MergedBulkWriteResult()
    {
        public MergedBulkWriteResult(params IBulkWriteResult[] baseResults) : this()
        {
            foreach (var baseResult in baseResults)
            {
                Results[baseResult.GetType().GenericTypeArguments[0]] = baseResult;
            }
            MatchedCount = baseResults.Sum(x => x.MatchedCount);
            DeletedCount = baseResults.Sum(x => x.DeletedCount);
            InsertedCount = baseResults.Sum(x => x.InsertedCount + x.Upserts?.Count ?? 0);
            ModifiedCount = baseResults.Sum(x => x.ModifiedCount);
        }

        public Dictionary<Type, IBulkWriteResult> Results { get; private set; } = new();

        public long MatchedCount { get; private set; }

        public long DeletedCount { get; private set; }
        /// <summary>
        /// Inserted data count, including upsert new data count
        /// </summary>
        public long InsertedCount { get; private set; }

        public long ModifiedCount { get; private set; }

        public static MergedBulkWriteResult operator +(MergedBulkWriteResult current, IBulkWriteResult newResult)
        {
            current.Results[newResult.GetType().GenericTypeArguments[0]] = newResult;
            current.MatchedCount += newResult.MatchedCount;
            current.DeletedCount += newResult.DeletedCount;
            current.InsertedCount += newResult.InsertedCount + newResult.Upserts?.Count ?? 0;
            current.ModifiedCount += newResult.ModifiedCount;
            return current;
        }
        public static MergedBulkWriteResult operator +(MergedBulkWriteResult current, object newResult)
        {
            var entityRootType = newResult.GetType().GenericTypeArguments[0];
            if (newResult is IBulkWriteResult bulkWriteResult)
            {
                current.Results[entityRootType] = bulkWriteResult;
                current.MatchedCount += bulkWriteResult.MatchedCount;
                current.DeletedCount += bulkWriteResult.DeletedCount;
                current.InsertedCount += bulkWriteResult.InsertedCount + bulkWriteResult.Upserts?.Count ?? 0;
                current.ModifiedCount += bulkWriteResult.ModifiedCount;
                return current;
            }
            throw new InvalidOperationException("Invalid bulk write result type: " + newResult.GetType().Name);
        }
    }

    public struct WriteResult
    {
        public static implicit operator WriteResult(UpdateResult result)
        {
            return result.IsAcknowledged switch
            {
                true => new WriteResult()
                {
                    UpsertedId = result.UpsertedId?.ToString(),
                    IsAcknowledged = true,
                    MatchedCount = result.MatchedCount,
                    ModifiedCount = result.ModifiedCount
                },
                _ => new WriteResult() { IsAcknowledged = false }
            };
        }

        public static implicit operator WriteResult(ReplaceOneResult result)
        {
            return result.IsAcknowledged switch
            {
                true => new WriteResult()
                {
                    UpsertedId = result.UpsertedId?.ToString(),
                    IsAcknowledged = true,
                    MatchedCount = result.MatchedCount,
                    ModifiedCount = result.ModifiedCount
                },
                _ => new WriteResult() { IsAcknowledged = false }
            };
        }

        /// <summary>
        /// Gets a value indicating whether the result is acknowledged.
        /// </summary>
        public bool IsAcknowledged { get; init; }

        /// <summary>
        /// Gets the matched count.
        /// </summary>
        public long MatchedCount { get; init; }

        /// <summary>
        /// Gets the modified count.
        /// </summary>
        public long ModifiedCount { get; init; }

        /// <summary>
        /// Gets the upserted id, if one exists.
        /// </summary>
        public string? UpsertedId { get; init; }
    }
}

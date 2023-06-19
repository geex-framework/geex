using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.BlobStorage.Core.Aggregates.BlobObjects;

using MongoDB.Driver;
using MongoDB.Entities;

namespace Geex.Common.BlobStorage.Migrations
{
    public class _637760693718958528_addIndex : DbMigration
    {
        /// <inheritdoc />
        public override async Task UpgradeAsync(DbContext dbContext)
        {
            await dbContext.Collection<BlobObject>().Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<BlobObject>(Builders<BlobObject>.IndexKeys.Ascending(x => x.Md5),new CreateIndexOptions() { Background = true}),
                new CreateIndexModel<BlobObject>(Builders<BlobObject>.IndexKeys.Ascending(x=>x.StorageType),new CreateIndexOptions() { Background = true}),
                new CreateIndexModel<BlobObject>(Builders<BlobObject>.IndexKeys.Ascending(x => x.MimeType).Ascending(x => x.FileName),new CreateIndexOptions() { Background = true}),
            });
        }
    }
}

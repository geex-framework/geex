using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Settings.Core;

using MongoDB.Driver;
using MongoDB.Entities;

namespace Geex.Common.BlobStorage.Migrations
{
    public class _637760700229756456_addIndex : DbMigration
    {
        /// <inheritdoc />
        public override async Task UpgradeAsync(DbContext dbContext)
        {

            await dbContext.Collection<Setting>().Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<Setting>(Builders<Setting>.IndexKeys.Ascending(x => x.Name).Ascending(x => x.Scope),new CreateIndexOptions() { Background = true}),
                new CreateIndexModel<Setting>(Builders<Setting>.IndexKeys.Ascending(x => x.ScopedKey), new CreateIndexOptions(){Sparse = true,Background = true}),
            });

        }
    }
}

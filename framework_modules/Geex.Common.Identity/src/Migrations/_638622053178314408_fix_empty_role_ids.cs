using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Entities;
using MongoDB.Driver;

namespace Geex.Common.Identity.Migrations
{
    public class _638622053178314408_fix_empty_role_ids : DbMigration
    {
        /// <inheritdoc />
        public override async Task UpgradeAsync(DbContext dbContext)
        {
            dbContext.DefaultDb.GetCollection<BsonDocument>("User").UpdateMany(dbContext.Session, Builders<BsonDocument>.Filter.Where(x=>x["RoleIds"] == default), Builders<BsonDocument>.Update.Set(x=>x["RoleIds"], new BsonArray()));
        }
    }
}

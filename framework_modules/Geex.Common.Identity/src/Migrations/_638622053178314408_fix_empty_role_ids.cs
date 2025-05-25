using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Entities;
using MongoDB.Driver;
using Geex.Abstractions.Migrations;

namespace Geex.Common.Identity.Migrations
{
    public class _638622053178314408_fix_empty_role_ids : DbMigration
    {
        /// <inheritdoc />
        public override long Number => long.Parse(this.GetType().Name.Split('_')[1]);
        public override async Task UpgradeAsync(IUnitOfWork uow)
        {
            uow.DbContext.DefaultDb.GetCollection<BsonDocument>("User").UpdateMany(uow.DbContext.Session, Builders<BsonDocument>.Filter.Not(Builders<BsonDocument>.Filter.Exists("RoleIds")), Builders<BsonDocument>.Update.Set(x => x["RoleIds"], new BsonArray()));
        }
    }
}

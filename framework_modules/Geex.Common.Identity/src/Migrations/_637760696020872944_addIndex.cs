using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.BlobStorage.Core.Aggregates.BlobObjects;
using Geex.Common.Identity.Api.Aggregates.Roles;
using Geex.Common.Identity.Core.Aggregates.Orgs;
using Geex.Common.Identity.Core.Aggregates.Users;

using MongoDB.Driver;
using MongoDB.Entities;

namespace Geex.Common.BlobStorage.Migrations
{
    public class _637760696020872944_addIndex : DbMigration
    {
        /// <inheritdoc />
        public override async Task UpgradeAsync(DbContext dbContext)
        {
            await dbContext.Collection<User>().Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.OrgCodes),new CreateIndexOptions() { Background = true}),
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.LoginProvider).Ascending(x => x.OpenId), new CreateIndexOptions(){Sparse = true,Background = true}),
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.Username).Ascending(x => x.PhoneNumber).Ascending(x => x.Email),new CreateIndexOptions() { Background = true}),
            });
            await dbContext.Collection<Role>().Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<Role>(Builders<Role>.IndexKeys.Ascending(x => x.Name),new CreateIndexOptions() { Background = true}),
            });
            await dbContext.Collection<Org>().Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<Org>(Builders<Org>.IndexKeys.Ascending(x => x.Code).Ascending(x => x.OrgType).Ascending(x => x.Name),new CreateIndexOptions() { Background = true }),
            });
        }
    }
}

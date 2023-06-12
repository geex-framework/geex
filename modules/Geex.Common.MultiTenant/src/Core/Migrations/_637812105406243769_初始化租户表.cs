using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Entities;
using Geex.Common.MultiTenant.Core.Aggregates.Tenants;
using MongoDB.Driver;

namespace Geex.Common.MultiTenant.Core.Migrations
{
    /// <summary>
    /// 初始化租户表
    /// </summary>
    public class _637812105406243769_初始化租户表 : DbMigration
    {
        public override async Task UpgradeAsync(DbContext dbContext)
        {
            await dbContext.Collection<Tenant>().Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<Tenant>(Builders<Tenant>.IndexKeys.Ascending(x => x.Code), new CreateIndexOptions() { Sparse = true,Background = true, Unique = true}),
            });
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Authorization.Casbin;

using MongoDB.Driver;
using MongoDB.Entities;

namespace Geex.Common.Authorization.Migrations
{
    public class _637760691775170587_addIndex : DbMigration
    {
        /// <inheritdoc />
        public override async Task UpgradeAsync(DbContext dbContext)
        {
            await dbContext.Collection<CasbinRule>().Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<CasbinRule>(Builders<CasbinRule>.IndexKeys.Ascending(x => x.PType),new CreateIndexOptions() { Background = true}),
                new CreateIndexModel<CasbinRule>(Builders<CasbinRule>.IndexKeys.Ascending(x => x.V0),new CreateIndexOptions() { Background = true}),

                new CreateIndexModel<CasbinRule>(Builders<CasbinRule>.IndexKeys.Ascending(x => x.V1),new CreateIndexOptions() { Background = true}),

            });
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using x_Org_x.x_Proj_x.x_Mod_x.Core.Aggregates.x_Aggregate_xs;

using MongoDB.Entities;

namespace x_Org_x.x_Proj_x.x_Mod_x.Tests
{
    public class TestData
    {
        public static List<x_Aggregate_x> x_aggregate_xs = new()
        {
            new x_Aggregate_x("test")
        };

        public class _637632330490465147_TestDataMigration : DbMigration
        {
            public override async Task UpgradeAsync(DbContext dbContext)
            {
                dbContext.Attach(x_aggregate_xs);
                await x_aggregate_xs.SaveAsync();
            }
        }
    }
}

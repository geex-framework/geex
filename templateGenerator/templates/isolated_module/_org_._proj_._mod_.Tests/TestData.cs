using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AutoFixture;

using Geex.Common.Testing;

using _org_._proj_._mod_.Core.Aggregates._aggregate_s;

using MongoDB.Entities;

namespace _org_._proj_._mod_.Tests
{
    public class TestData
    {
        public static List<_aggregate_> _aggregate_s = new()
        {
            new _aggregate_("test")
        };

        public class _637632330490465147_TestDataMigration : IMigration
        {
            public override async Task UpgradeAsync(DbContext dbContext)
            {
                dbContext.Attach(_aggregate_s);
                await _aggregate_s.SaveAsync();
            }
        }
    }
}

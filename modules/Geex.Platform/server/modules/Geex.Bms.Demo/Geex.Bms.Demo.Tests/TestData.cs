using System.Collections.Generic;
using System.Threading.Tasks;
using Geex.Bms.Demo.Core.Aggregates.Books;

using MongoDB.Entities;

namespace Geex.Bms.Demo.Tests
{
    public class TestData
    {
        public static List<Book> books = new()
        {
            new Book("test")
        };

        public class _637632330490465147_TestDataMigration : DbMigration
        {
            public override async Task UpgradeAsync(DbContext dbContext)
            {
                dbContext.Attach(books);
                await books.SaveAsync();
            }
        }
    }
}

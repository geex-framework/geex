using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    public class _001_rename_field : DbMigration
    {

        public override async Task UpgradeAsync(DbContext dbContext)
        {
            await dbContext.Update<Book>()
              .Match(_ => true)
              .Modify(b => b.Rename("SellingPrice", "Price"))
              .ExecuteAsync().ConfigureAwait(false);
        }
    }
}

using System.Threading.Tasks;

namespace MongoDB.Entities.Tests
{
    public class _002_undo_field_rename : DbMigration
    {
        public override async Task UpgradeAsync(DbContext dbContext)
        {
            await dbContext.Update<Book>()
             .Match(_ => true)
             .Modify(b => b.Rename("Price", "SellingPrice"))
             .ExecuteAsync().ConfigureAwait(false);
        }
    }
}

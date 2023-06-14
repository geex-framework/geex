using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace MongoDB.Entities
{
    /// <summary>
    /// The contract for writing user data migration classes
    /// </summary>
    [ExposeServices(typeof(DbMigration))]
    public abstract class DbMigration : ITransientDependency
    {
        public abstract Task UpgradeAsync(DbContext dbContext);
    }
}

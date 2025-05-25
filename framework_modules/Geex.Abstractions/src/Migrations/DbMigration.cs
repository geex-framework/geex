using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Geex.Migrations
{
    /// <summary>
    /// The contract for writing user data migration classes
    /// </summary>
    [ExposeServices(typeof(DbMigration))]
    public abstract class DbMigration : ITransientDependency
    {
        public abstract long Number { get; }
        public abstract Task UpgradeAsync(IUnitOfWork dbContext);
    }
}

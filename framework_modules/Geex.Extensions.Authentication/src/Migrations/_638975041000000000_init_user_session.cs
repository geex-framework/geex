using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Extensions.Authentication.Core.Entities;
using Geex.Migrations;
using MongoDB.Entities;

namespace Geex.Extensions.Authentication.Migrations;

public class _638975041000000000_init_user_session : DbMigration
{
    public override long Number => long.Parse(GetType().Name.Split('_')[1]);

    public override Task UpgradeAsync(IUnitOfWork uow)
    {
        _ = DB.Collection<UserSession>();
        return Task.CompletedTask;
    }
}

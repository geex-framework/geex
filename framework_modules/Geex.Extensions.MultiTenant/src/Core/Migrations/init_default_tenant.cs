using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Extensions.Authentication;
using Geex.Extensions.Identity;
using Geex.Extensions.MultiTenant.Core.Aggregates.Tenants;
using Geex.Extensions.Requests.MultiTenant;
using Geex.Migrations;
using Geex.MultiTenant;

using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.MultiTenant.Core.Migrations
{
    public class init_default_tenant : DbMigration
    {
        /// <inheritdoc />
        public override long Number { get; } = 638851762300008568;

        /// <inheritdoc />
        public override async Task UpgradeAsync(IUnitOfWork uow)
        {
            var tenant = uow.Create(new CreateTenantRequest()
            {
                Code = "default",
                Name = "默认租户",
            });

            var users = uow.Query<IUser>().Where(x => x.Id != GeexConstants.SuperAdminId);
            foreach (var user in users)
            {
                user.SetTenant(tenant.Code);
            }

            var roles = uow.Query<IRole>();
            foreach (var role in roles)
            {
                role.SetTenant(tenant.Code);
            }

            var orgs = uow.Query<IOrg>();
            foreach (var role in orgs)
            {
                role.SetTenant(tenant.Code);
            }
        }
    }
}

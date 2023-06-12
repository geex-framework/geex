using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Authorization;
using Geex.Common.Authorization.Events;
using Geex.Common.Identity.Api.Aggregates.Roles;
using Geex.Common.Identity.Api.Aggregates.Users;
using Geex.Common.Identity.Core.Aggregates.Orgs;
using Geex.Common.Identity.Core.Aggregates.Users;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using MongoDB.Entities;

using NetCasbin.Abstractions;

namespace Geex.Core.Authentication.Migrations
{
    public class _20210705110119_init_admin : DbMigration
    {
        public override async Task UpgradeAsync(DbContext dbContext)
        {
            await dbContext.SaveChanges();
            var superAdmin = User.New(dbContext.ServiceProvider.GetService<IUserCreationValidator>(), dbContext.ServiceProvider.GetService<IPasswordHasher<IUser>>(), "superAdmin", "superAdmin", "15055555555", "superAdmin@geex.com", "superAdmin");
            dbContext.Attach(superAdmin);
            superAdmin.Id = "000000000000000000000001";
            await dbContext.SaveChanges();
            var adminRole = new Role("admin")
            {
                IsStatic = true
            };
            var userRole = new Role("user")
            {
                IsDefault = true,
                IsStatic = true,
            };
            var roles = new List<Role>()
            {
                adminRole,
                userRole
            };
            dbContext.Attach(roles);

            var orgs = new List<Org>()
            {
                new Org("geex","geex", OrgTypeEnum.Default )
            };
            dbContext.Attach(orgs);

            var admin = User.New(dbContext.ServiceProvider.GetService<IUserCreationValidator>(), dbContext.ServiceProvider.GetService<IPasswordHasher<IUser>>(), "admin", "admin", "13333333332", "admin@geex.com", "admin");
            dbContext.Attach(admin);
            await admin.AssignOrgs(orgs);
            await admin.AssignRoles(adminRole.Id);
            var user = User.New(dbContext.ServiceProvider.GetService<IUserCreationValidator>(), dbContext.ServiceProvider.GetService<IPasswordHasher<IUser>>(), "user", "user", "15555555555", "user@geex.com", "user");
            dbContext.Attach(user);
            await user.AssignRoles(userRole.Id);
            await user.AssignOrgs(orgs);
            var permissions = AppPermission.List.Select(x => x.Value);
            await dbContext.ServiceProvider.GetService<IRbacEnforcer>().SetPermissionsAsync(adminRole.Id, permissions);
            await dbContext.ServiceProvider.GetService<IMediator>().Publish(new PermissionChangedEvent(adminRole.Id, permissions.ToArray()));
        }
    }
}

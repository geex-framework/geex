using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Authorization;
using Geex.Entities;
using Geex.Extensions.Authorization;
using Geex.Extensions.Authorization.Events;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Requests;
using Geex.Migrations;
using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using MongoDB.Entities;

using NetCasbin.Abstractions;

namespace Geex.Core.Authentication.Migrations
{
    public class _20210705110119_init_admin : DbMigration
    {
        /// <inheritdoc />
        public override long Number => long.Parse(this.GetType().Name.Split('_')[1]);

        public override async Task  UpgradeAsync(IUnitOfWork uow)
        {
            var superAdmin = uow.Create(new CreateUserRequest()
            {
                Username = "superAdmin",
                Nickname = "superAdmin",
                PhoneNumber = "15055555555",
                Email = "superAdmin@geex.com",
                Password = "superAdmin"
            });
            superAdmin.Id = "000000000000000000000001";
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
            uow.Attach(roles);

            var orgs = new List<Org>()
            {
                new Org("geex","geex", OrgTypeEnum.Default )
            };
            uow.Attach(orgs);

            var admin = uow.Create(new CreateUserRequest()
            {
                Username = "admin",
                Nickname = "admin",
                PhoneNumber = "13333333332",
                Email = "admin@geex.com",
                Password = "admin"
            });
            await admin.AssignOrgs(orgs);
            await admin.AssignRoles(adminRole.Id);
            var user = uow.Create(new CreateUserRequest()
            {
                Username = "user",
                Nickname = "user",
                PhoneNumber = "15555555555",
                Email = "user@geex.com",
                Password = "user"
            });
            await user.AssignRoles(userRole.Id);
            await user.AssignOrgs(orgs);
            var permissions = AppPermission.List.Select(x => x.Value);
            await uow.ServiceProvider.GetService<IRbacEnforcer>().SetPermissionsAsync(adminRole.Id, permissions);
            await uow.ServiceProvider.GetService<IUnitOfWork>().Notify(new PermissionChangedEvent(adminRole.Id, permissions.ToArray()));
        }
    }
}

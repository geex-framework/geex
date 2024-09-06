using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Events;
using Geex.Common.Abstractions;
using Geex.Common.Identity.Api.Aggregates.Orgs.Events;
using Geex.Common.Identity.Api.Aggregates.Roles;
using Geex.Common.Identity.Api.Aggregates.Users;
using Geex.Common.Identity.Core.Aggregates.Orgs;

using GreenDonut;

using HotChocolate.Types;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using MongoDB.Bson.Serialization;


namespace Geex.Common.Identity.Core.Aggregates.Users
{
    public partial class User : Abstraction.Storage.Entity<User>, IUser
    {
        public string? PhoneNumber { get; set; }
        public bool IsEnable { get; set; }
        public string Username { get; set; }
        public string? Nickname { get; set; }
        public string? Email { get; set; }
        public string Password { get; set; }
        public List<UserClaim> Claims { get; set; }
        public IQueryable<IOrg> Orgs => DbContext.Query<IOrg>().Where(x => this.OrgCodes.Contains(x.Code));
        public List<string> OrgCodes { get; set; }
        public List<string> Permissions => DbContext.ServiceProvider.GetService<IMediator>().Send(new GetSubjectPermissionsRequest(this.Id)).Result.ToList();
        public void ChangePassword(string originPassword, string newPassword)
        {
            if (!this.CheckPassword(originPassword))
            {
                throw new BusinessException(GeexExceptionType.OnPurpose, message: "原密码校验失败.");
            }
            this.SetPassword(newPassword);
        }

        public List<string> RoleIds => DbContext.ServiceProvider.GetService<IRbacEnforcer>().GetRolesForUser(this.Id);
        public Lazy<IBlobObject?> AvatarFile => LazyQuery(() => AvatarFile);
        public string? AvatarFileId { get; set; }

        public IQueryable<IRole> Roles => DbContext.Query<Role>().Where(x => this.RoleIds.Contains(x.Id));
        public List<string> RoleNames
        {
            get
            {
                var roleNames = Roles.Select(x => x.Name).ToList();
                return roleNames;
            }
        }
        [JsonConstructor]
        protected User()
        {
            IsEnable = true;
            Claims = Enumerable.Empty<UserClaim>().ToList();
            OrgCodes = Enumerable.Empty<string>().ToList();
            ConfigLazyQuery(x => x.AvatarFile, blob => blob.Id == AvatarFileId, users => blob => users.SelectList(x => x.AvatarFileId).Contains(blob.Id));
        }

        public User(IUserCreationValidator userCreationValidator, IPasswordHasher<IUser> passwordHasher, string username, string nickname, string phoneNumber, string email, string password) : this()
        {
            this.Username = username;
            this.Email = email;
            this.PhoneNumber = phoneNumber;
            this.LoginProvider = LoginProviderEnum.Local;
            userCreationValidator.Check(this);
            this.Password = passwordHasher.HashPassword(this, password);
        }

        public User(IUserCreationValidator userCreationValidator, IPasswordHasher<IUser> passwordHasher, string username, string nickname, LoginProviderEnum loginProvider, string openId, string? phoneNumber = default, string? email = default, string? password = default) : this()
        {
            this.Username = username;
            this.Nickname = nickname;
            this.Email = email ?? username + "@x_org_x.com";
            this.PhoneNumber = phoneNumber;
            this.LoginProvider = loginProvider;
            this.OpenId = openId;
            userCreationValidator.Check(this);
            this.Password = passwordHasher.HashPassword(this, password ?? Guid.NewGuid().ToString("N").Substring(0, 16));
        }

        public virtual bool CheckPassword(string password)
        {
            var passwordHasher = this.ServiceProvider.GetService<IPasswordHasher<IUser>>();
            return passwordHasher!.VerifyHashedPassword(this, Password, password) != PasswordVerificationResult.Failed;
        }
        public virtual async Task AssignRoles(IEnumerable<IRole> roles)
        {
            await this.AssignRoles(roles.Select(x => x.Id).ToList());
        }

        public virtual async Task AssignOrgs(IEnumerable<IOrg> orgs)
        {
            this.OrgCodes = orgs.Select(x => x.Code).ToList();
            this.AddDomainEvent(new UserOrgChangedEvent(this.Id, orgs.Select(x => x.Code).ToList()));
        }

        public virtual async Task AssignRoles(IEnumerable<string> roles)
        {
            await this.ServiceProvider.GetService<IMediator>().Send(new UserRoleChangeRequest(this.Id, roles.ToList()));
        }

        public virtual async Task AssignOrgs(IEnumerable<string> orgs)
        {
            this.OrgCodes = orgs.ToList();
            this.AddDomainEvent(new UserOrgChangedEvent(this.Id, orgs.ToList()));
        }

        public virtual IUser SetPassword(string? password)
        {
            Password = ServiceProvider.GetService<IPasswordHasher<IUser>>().HashPassword(this, password);
            return this;
        }

        public virtual Task AssignRoles(params string[] roles)
        {
            return this.AssignRoles(roles.ToList());
        }

        public virtual async Task AddOrg(IOrg entity)
        {
            this.OrgCodes.AddIfNotContains(entity.Code);
            this.AddDomainEvent(new UserOrgChangedEvent(this.Id, this.OrgCodes.ToList()));
        }

        public User(IUserCreationValidator userCreationValidator, IPasswordHasher<IUser> passwordHasher, string openId, LoginProviderEnum loginProvider, string username, string? phoneNumber = default, string? email = default, string? password = default)
        {
            this.Username = username;
            this.OpenId = openId;
            this.LoginProvider = loginProvider;
            this.PhoneNumber = phoneNumber;
            this.Email = email;
            userCreationValidator.Check(this);
            if (!password.IsNullOrEmpty())
            {
                this.Password = passwordHasher.HashPassword(this, password);
            }
        }

        public LoginProviderEnum LoginProvider { get; set; }

        public string? OpenId { get; set; }
        public string? TenantCode { get; set; }
    }
}
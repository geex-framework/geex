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
using Geex.Common.Requests.Identity;

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
        [JsonConstructor]
        protected User()
        {
            IsEnable = true;
            ConfigLazyQuery(x => x.AvatarFile, blob => blob.Id == AvatarFileId, users => blob => users.SelectList(x => x.AvatarFileId).Contains(blob.Id));
        }

        public User(IUserCreationValidator userCreationValidator, IPasswordHasher<IUser> passwordHasher,
            ICreateUserRequest request)
        {
            this.Username = request.Username;
            this.OpenId = request.OpenId;
            this.Email = request.Email ?? request.Username + "@geexorggeex.com";
            this.PhoneNumber = request.PhoneNumber;
            this.LoginProvider = request.Provider ?? LoginProviderEnum.Local;
            this.Nickname = request.Nickname;
            this.AvatarFileId = request.AvatarFileId;
            this.IsEnable = request.IsEnable;
            userCreationValidator.Check(this);
            this.Password = passwordHasher.HashPassword(this, request.Password);
        }

        public string Password { get; set; }
        public string? TenantCode { get; set; }

        public virtual async Task AddOrg(IOrg entity)
        {
            this.OrgCodes.AddIfNotContains(entity.Code);
            this.AddDomainEvent(new UserOrgChangedEvent(this.Id, this.OrgCodes.ToList()));
        }

        public virtual async Task AssignOrgs(IEnumerable<IOrg> orgs)
        {
            this.OrgCodes = orgs.Select(x => x.Code).ToList();
            this.AddDomainEvent(new UserOrgChangedEvent(this.Id, orgs.Select(x => x.Code).ToList()));
        }

        public virtual async Task AssignOrgs(IEnumerable<string> orgs)
        {
            this.OrgCodes = orgs.ToList();
            this.AddDomainEvent(new UserOrgChangedEvent(this.Id, orgs.ToList()));
        }

        public virtual async Task AssignRoles(IEnumerable<IRole> roles)
        {
            await this.AssignRoles(roles.Select(x => x.Id).ToList());
        }

        public virtual async Task AssignRoles(IEnumerable<string> roles)
        {
            this.RoleIds = roles.ToList();
            await this.ServiceProvider.GetService<IUnitOfWork>().Request(new UserRoleChangeRequest(this.Id, roles.ToList()));
        }

        public virtual Task AssignRoles(params string[] roles)
        {
            return this.AssignRoles(roles.ToList());
        }

        public Lazy<IBlobObject?> AvatarFile => LazyQuery(() => AvatarFile);
        public string? AvatarFileId { get; set; }

        public void ChangePassword(string originPassword, string newPassword)
        {
            if (!this.CheckPassword(originPassword))
            {
                throw new BusinessException(GeexExceptionType.OnPurpose, message: "原密码校验失败.");
            }
            this.SetPassword(newPassword);
        }

        public virtual bool CheckPassword(string password)
        {
            var passwordHasher = this.ServiceProvider.GetService<IPasswordHasher<IUser>>();
            return passwordHasher!.VerifyHashedPassword(this, Password, password) != PasswordVerificationResult.Failed;
        }

        public List<UserClaim> Claims { get; set; } = new List<UserClaim>();
        public string? Email { get; set; }
        public bool IsEnable { get; set; }

        public LoginProviderEnum LoginProvider { get; set; }
        public string? Nickname { get; set; }

        public string? OpenId { get; set; }
        public List<string> OrgCodes { get; set; } = new List<string>();
        public IQueryable<IOrg> Orgs => DbContext.Query<IOrg>().Where(x => this.OrgCodes.Contains(x.Code));
        public List<string> Permissions => DbContext.ServiceProvider.GetService<IUnitOfWork>().Request(new GetSubjectPermissionsRequest(this.Id)).Result.ToList();
        public string? PhoneNumber { get; set; }

        public List<string> RoleIds { get; internal set; } = new List<string>();

        public List<string> RoleNames
        {
            get
            {
                var roleNames = Roles.Select(x => x.Name).ToList();
                return roleNames;
            }
        }

        public IQueryable<IRole> Roles => DbContext.Query<Role>().Where(x => this.RoleIds.Contains(x.Id));

        public virtual IUser SetPassword(string? password)
        {
            Password = ServiceProvider.GetService<IPasswordHasher<IUser>>().HashPassword(this, password);
            return this;
        }

        public string Username { get; set; }
    }
}

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
        public IQueryable<IOrg> Orgs => DbContext.Query<Org>().Where(x => this.OrgCodes.Contains(x.Code));
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

        public static User New(IUserCreationValidator userCreationValidator, IPasswordHasher<IUser> passwordHasher, string username, string nickname, string phoneNumber, string email, string password)
        {
            var result = new User()
            {
                Username = username,
                Email = email,
                PhoneNumber = phoneNumber,
                LoginProvider = LoginProviderEnum.Local
            };
            userCreationValidator.Check(result);
            result.Password = passwordHasher.HashPassword(result, password);
            return result;
        }

        public static User New(IUserCreationValidator userCreationValidator, IPasswordHasher<IUser> passwordHasher, string username, string nickname, LoginProviderEnum loginProvider, string openId, string? phoneNumber = default, string? email = default, string? password = default)
        {
            var result = new User()
            {
                Username = username,
                Nickname = nickname,
                Email = email ?? username + "@x_org_x.com",
                PhoneNumber = phoneNumber,
                LoginProvider = loginProvider,
                OpenId = openId,
            };
            userCreationValidator.Check(result);
            result.Password = passwordHasher.HashPassword(result, password ?? Guid.NewGuid().ToString("N").Substring(0, 16));
            return result;
        }

        public virtual bool CheckPassword(string password)
        {
            var passwordHasher = this.ServiceProvider.GetService<IPasswordHasher<IUser>>();
            return passwordHasher!.VerifyHashedPassword(this, Password, password) != PasswordVerificationResult.Failed;
        }
        public async Task AssignRoles(IEnumerable<IRole> roles)
        {
            await this.AssignRoles(roles.Select(x => x.Id).ToList());
        }

        public async Task AssignOrgs(List<Org> orgs)
        {
            this.OrgCodes = orgs.Select(x => x.Code).ToList();
            this.AddDomainEvent(new UserOrgChangedEvent(this.Id, orgs.Select(x => x.Code).ToList()));
        }

        public async Task AssignRoles(List<string> roles)
        {
            await this.ServiceProvider.GetService<IMediator>().Send(new UserRoleChangeRequest(this.Id, roles.ToList()));
        }

        public async Task AssignOrgs(List<string> orgs)
        {
            this.OrgCodes = orgs.ToList();
            this.AddDomainEvent(new UserOrgChangedEvent(this.Id, orgs.ToList()));
        }

        public User SetPassword(string? password)
        {
            Password = ServiceProvider.GetService<IPasswordHasher<IUser>>().HashPassword(this, password);
            return this;
        }

        public Task AssignRoles(params string[] roles)
        {
            return this.AssignRoles(roles.ToList());
        }

        public async Task AddOrg(IOrg entity)
        {
            this.OrgCodes.AddIfNotContains(entity.Code);
            this.AddDomainEvent(new UserOrgChangedEvent(this.Id, this.OrgCodes.ToList()));
        }

        public static User NewExternal(IUserCreationValidator userCreationValidator, IPasswordHasher<IUser> passwordHasher, string openId, LoginProviderEnum loginProvider, string username, string? phoneNumber = default, string? email = default, string? password = default)
        {
            var result = new User()
            {
                Username = username,
                OpenId = openId,
                LoginProvider = loginProvider,
                PhoneNumber = phoneNumber,
                Email = email,
            };
            userCreationValidator.Check(result);
            if (!password.IsNullOrEmpty())
            {
                result.Password = passwordHasher.HashPassword(result, password);
            }
            return result;
        }

        public LoginProviderEnum LoginProvider { get; set; }

        public string? OpenId { get; set; }
        public string? TenantCode { get; set; }
        public override async Task<ValidationResult> Validate(IServiceProvider sp, CancellationToken cancellation = default)
        {
            return ValidationResult.Success;
        }

        public class UserBsonConfig : BsonConfig<User>
        {
            protected override void Map(BsonClassMap<User> map, BsonIndexConfig<User> indexConfig)
            {
                map.Inherit<IUser>();
                map.SetIsRootClass(true);
                map.AutoMap();
                indexConfig.MapEntityDefaultIndex();
                indexConfig.MapIndex(x => x.Ascending(y => y.OpenId), options =>
                {
                    options.Background = true;
                    options.Sparse = true;
                });
                indexConfig.MapIndex(x => x.Ascending(y => y.Email), options =>
                {
                    options.Background = true;
                });
                indexConfig.MapIndex(x => x.Ascending(y => y.Username), options =>
                {
                    options.Background = true;
                });
                indexConfig.MapIndex(x => x.Hashed(y => y.LoginProvider), options =>
                {
                    options.Background = true;
                    options.Sparse = true;
                });
                indexConfig.MapIndex(x => x.Ascending(y => y.PhoneNumber), options =>
                {
                    options.Background = true;
                });
            }
        }
        public class UserGqlConfig : GqlConfig.Object<User>
        {
            /// <inheritdoc />
            protected override void Configure(IObjectTypeDescriptor<User> descriptor)
            {
                descriptor.Implements<InterfaceType<IUser>>();
                descriptor.AuthorizeFieldsImplicitly();
                descriptor.BindFieldsImplicitly();
                descriptor.ConfigEntity();
                //descriptor.Field(x => x.UserName);
                //descriptor.Field(x => x.IsEnable);
                //descriptor.Field(x => x.Email);
                //descriptor.Field(x => x.PhoneNumber);
                //descriptor.Field(x => x.Roles);
                //descriptor.Field(x => x.Orgs);
                descriptor.Field(x => x.Claims).UseFiltering<UserClaim>(x =>
                {
                    x.Field(y => y.ClaimType);
                });
                //descriptor.Ignore(x => x.Claims);
                //descriptor.Ignore(x => x.AuthorizedPermissions);
            }
        }
    }
}
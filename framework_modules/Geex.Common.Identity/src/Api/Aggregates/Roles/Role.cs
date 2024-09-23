using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Authorization;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Identity.Core.Aggregates.Users;

using HotChocolate.Types;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Bson.Serialization;


namespace Geex.Common.Identity.Api.Aggregates.Roles
{
    /// <summary>
    /// role为了方便和string的相互转化, 采用class的形式
    /// </summary>
    public class Role : Geex.Common.Abstraction.Storage.Entity<Role>, ITenantFilteredEntity, IRole
    {
        public string Name { get; set; }
        public string Code { get; set; }

        public Role(string code)
        {
            this.Name = code;
            this.Code = code;
        }

        public Role(string roleCode, string roleName, bool isStatic = false, bool isDefault = false) : this(roleCode)
        {
            this.Name = roleName;
            this.IsStatic = isStatic;
            this.IsDefault = isDefault;
        }

        public IQueryable<IUser> Users
        {
            get
            {
                var userIds = DbContext.ServiceProvider.GetService<IRbacEnforcer>().GetUsersForRole(this.Id);
                return DbContext.Query<User>().Where(x => userIds.Contains(x.Id));
            }
        }
        public List<string> Permissions => DbContext.ServiceProvider.GetService<IUnitOfWork>().Request(new GetSubjectPermissionsRequest(this.Id)).Result.ToList();

        public string? TenantCode { get; set; }
        public bool IsDefault { get; set; }
        public bool IsStatic { get; set; }
        public bool IsEnabled { get; set; } = true;

        public override async Task<ValidationResult> Validate(IServiceProvider sp, CancellationToken cancellation = default)
        {
            var duplicateRole = this.DbContext.Query<Role>()
                .FirstOrDefault(x => x.Code == this.Code && x.TenantCode == this.TenantCode && x.Id != this.Id);
            if (duplicateRole != default)
            {
                return new ValidationResult($"当前租户[{sp.GetService<ICurrentTenant>()?.Code}]下角色重复:{this.Code}, [{this.Id} != {duplicateRole.Id}]");
            }
            return ValidationResult.Success;
        }

        public class RoleBsonConfig : BsonConfig<Role>
        {
            protected override void Map(BsonClassMap<Role> map, BsonIndexConfig<Role> indexConfig)
            {
                map.Inherit<IRole>();
                map.AutoMap();
                indexConfig.MapEntityDefaultIndex();
                indexConfig.MapIndex(x => x.Ascending(y => y.Name), options => options.Background = true);
                indexConfig.MapIndex(x => x.Ascending(y => y.Code), options => options.Background = true);
            }
        }
        public class RoleGqlConfig : GqlConfig.Object<Role>
        {
            /// <inheritdoc />
            protected override void Configure(IObjectTypeDescriptor<Role> descriptor)
            {
                descriptor.BindFieldsImplicitly();
                //descriptor.Field(x => x.Users).Type<ListType<UserType>>().Resolve(x=>x.ToString());
                descriptor.ConfigEntity();
                descriptor.AuthorizeFieldsImplicitly();
            }
        }
    }
}

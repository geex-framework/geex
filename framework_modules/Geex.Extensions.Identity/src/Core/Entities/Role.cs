using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Extensions.Authorization;
using Geex.MultiTenant;
using Geex.Storage;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.Identity.Core.Entities
{
    /// <summary>
    /// role为了方便和string的相互转化, 采用class的形式
    /// </summary>
    public partial class Role : Entity<Role>, ITenantFilteredEntity, IRole
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

        public override async Task<ValidationResult> Validate(CancellationToken cancellation = default)
        {
            var duplicateRole = this.DbContext.Query<Role>()
                .FirstOrDefault(x => x.Code == this.Code && x.TenantCode == this.TenantCode && x.Id != this.Id);
            if (duplicateRole != default)
            {
                return new ValidationResult($"当前租户[{this.ServiceProvider.GetService<ICurrentTenant>()?.Code}]下角色重复:{this.Code}, [{this.Id} != {duplicateRole.Id}]");
            }
            return ValidationResult.Success;
        }
    }
}

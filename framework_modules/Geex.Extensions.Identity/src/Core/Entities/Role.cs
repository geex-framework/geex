using System;
using System.Collections.Generic;
using Geex.Validation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Authorization;
using Geex.MultiTenant;
using Geex.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Identity.Core.Entities
{
    /// <summary>
    /// role为了方便和string的相互转化, 采用class的形式
    /// </summary>
    public partial class Role : Entity<Role>, ITenantFilteredEntity, IRole
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }

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

        /// <summary>
        ///     更新角色信息
        /// </summary>
        /// <param name="name">角色名称</param>
        /// <param name="code">角色编码</param>
        /// <param name="description">角色描述</param>
        /// <param name="isDefault">是否默认角色</param>
        /// <param name="isStatic">是否静态角色</param>
        /// <param name="isEnabled">是否启用</param>
        public void UpdateRole(string? name = null, string? code = null, string? description = null, bool? isDefault = null, bool? isStatic = null, bool? isEnabled = null)
        {
            if (!name.IsNullOrEmpty())
                Name = name;

            if (!code.IsNullOrEmpty())
                Code = code;

            if (description != null)
                Description = description;

            if (isDefault.HasValue)
                IsDefault = isDefault.Value;

            if (isStatic.HasValue)
                IsStatic = isStatic.Value;

            if (isEnabled.HasValue)
                IsEnabled = isEnabled.Value;
        }

        /// <summary>
        ///     复制角色权限
        /// </summary>
        /// <param name="fromRole">源角色</param>
        public void CopyPermissionsFrom(IRole fromRole)
        {
            var enforcer = DbContext.ServiceProvider.GetService<IRbacEnforcer>();
            if (enforcer == null) return;

            // 获取源角色的权限
            var permissions = enforcer.GetImplicitPermissionsForUser(fromRole.Id);

            // 清除当前角色的权限并设置新权限
            enforcer.SetPermissionsAsync(this.Id, permissions).Wait();
        }

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

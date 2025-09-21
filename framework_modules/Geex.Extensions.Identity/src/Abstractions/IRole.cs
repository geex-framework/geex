using System.Collections.Generic;
using System.Linq;
using Geex.MultiTenant;

namespace Geex.Extensions.Identity
{
    public interface IRole : ITenantFilteredEntity
    {
        string Name { get; set; }
        string Code { get; set; }
        string? Description { get; set; }
        IQueryable<IUser> Users { get; }
        List<string> Permissions { get; }
        bool IsDefault { get; set; }
        bool IsStatic { get; set; }
        bool IsEnabled { get; set; }

        /// <summary>
        ///     更新角色信息
        /// </summary>
        /// <param name="name">角色名称</param>
        /// <param name="code">角色编码</param>
        /// <param name="description">角色描述</param>
        /// <param name="isDefault">是否默认角色</param>
        /// <param name="isStatic">是否静态角色</param>
        /// <param name="isEnabled">是否启用</param>
        void UpdateRole(string? name = null, string? code = null, string? description = null, bool? isDefault = null, bool? isStatic = null, bool? isEnabled = null);

        /// <summary>
        ///     复制角色权限
        /// </summary>
        /// <param name="fromRole">源角色</param>
        void CopyPermissionsFrom(IRole fromRole);
    }
}

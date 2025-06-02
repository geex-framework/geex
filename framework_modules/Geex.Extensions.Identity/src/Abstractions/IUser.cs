using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geex.Extensions.Authentication;
using Geex.Extensions.BlobStorage;
using Geex.MultiTenant;
using Geex.Storage;
using MongoDB.Entities;

namespace Geex.Extensions.Identity
{
    public interface IUser : IAuthUser, IEntity, ITenantFilteredEntity
    {
        List<string> RoleIds { get; }
        List<string> OrgCodes { get; set; }
        public List<string> Permissions { get; }
        List<UserClaim> Claims { get; set; }
        IQueryable<IOrg> Orgs { get; }
        Lazy<IBlobObject?> AvatarFile { get; }
        string? AvatarFileId { get; set; }
        IQueryable<IRole> Roles { get; }
        List<string> RoleNames { get; }
        Task AssignRoles(IEnumerable<IRole> roles);
        Task AssignOrgs(IEnumerable<IOrg> orgs);
        Task AssignRoles(IEnumerable<string> roles);
        Task AssignOrgs(IEnumerable<string> orgs);
        Task AssignRoles(params string[] roles);
        Task AddOrg(IOrg entity);
        new IUser SetPassword(string? password);
        // 显式实现父接口方法，委托给子接口的实现
        IAuthUser IAuthUser.SetPassword(string? password)
        {
            return SetPassword(password); // 调用子接口的实现
        }
    }
}

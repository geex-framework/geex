using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geex.Common.Abstraction.MultiTenant;

using MongoDB.Entities;

namespace Geex.Common.Abstraction.Entities
{
    public interface IUser : IEntityBase, ITenantFilteredEntity
    {
        public const string SuperAdminName = "superAdmin";
        public const string SuperAdminId = "000000000000000000000001";
        string? PhoneNumber { get; set; }
        string Username { get; set; }
        string? Nickname { get; set; }
        string? Email { get; set; }
        LoginProviderEnum LoginProvider { get; set; }
        string? OpenId { get; set; }
        public bool IsEnable { get; set; }
        List<string> RoleIds { get; }
        List<string> OrgCodes { get; set; }
        public List<string> Permissions { get; }
        List<UserClaim> Claims { get; set; }
        IQueryable<IOrg> Orgs { get; }
        Lazy<IBlobObject?> AvatarFile { get; }
        string? AvatarFileId { get; set; }
        IQueryable<IRole> Roles { get; }
        List<string> RoleNames { get; }
        void ChangePassword(string originPassword, string newPassword);
        bool CheckPassword(string password);
        Task AssignRoles(IEnumerable<IRole> roles);
        Task AssignOrgs(IEnumerable<IOrg> orgs);
        Task AssignRoles(IEnumerable<string> roles);
        Task AssignOrgs(IEnumerable<string> orgs);
        IUser SetPassword(string? password);
        Task AssignRoles(params string[] roles);
        Task AddOrg(IOrg entity);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
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
        void ChangePassword(string originPassword, string newPassword);
        bool CheckPassword(string password);
    }
}

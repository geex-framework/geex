using System.Collections.Generic;
using System.Linq;
using Geex.MultiTenant;

namespace Geex.Extensions.Identity
{
    public interface IRole : ITenantFilteredEntity
    {
        string Name { get; set; }
        string Code { get; set; }
        IQueryable<IUser> Users { get; }
        List<string> Permissions { get; }
        bool IsDefault { get; set; }
        bool IsStatic { get; set; }
        bool IsEnabled { get; set; }
    }
}

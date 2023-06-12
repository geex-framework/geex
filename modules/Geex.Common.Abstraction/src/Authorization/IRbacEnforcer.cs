using System.Collections.Generic;
using System.Threading.Tasks;
using Geex.Common.Authorization;
using NetCasbin.Abstractions;

namespace Geex.Common.Abstraction.Authorization
{
    public interface IRbacEnforcer
    {
        List<string> GetRolesForUser(string sub);
        List<string> GetUsersForRole(string sub);
        Task SetRoles(string sub, List<string> roles);
        Task SetPermissionsAsync(string sub, IEnumerable<string> permissions);
        Task<bool> AddRolesForUserAsync(string sub, IEnumerable<string> role);
        List<string> GetImplicitPermissionsForUser(string sub);
        bool Enforce(string sub, string mod, string act, string obj, string fields = "");
        Task<bool> EnforceAsync(string sub, string mod, string act, string obj, string fields = "");
        Task<bool> EnforceAsync(string sub, AppPermission permission);
        bool Enforce(string sub, AppPermission permission);
    }
}
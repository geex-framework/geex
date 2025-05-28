using System.Linq;

using Microsoft.AspNetCore.Authorization;

namespace Geex.Extensions.Authorization.Casbin
{
    public class CasbinRequirement : IAuthorizationRequirement
    {
        public string Obj { get; }
        public string Field { get; set; }

        public CasbinRequirement(string policyName)
        {
            var split = policyName.Split('_');
            this.Obj = split.ElementAtOrDefault(0) ?? "";
            this.Field = split.ElementAtOrDefault(1) ?? "";
        }

    }
}

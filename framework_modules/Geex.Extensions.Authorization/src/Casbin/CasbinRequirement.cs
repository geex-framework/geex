using System.Linq;

using Microsoft.AspNetCore.Authorization;

namespace Geex.Extensions.Authorization.Casbin
{
    public class CasbinRequirement : IAuthorizationRequirement
    {
        public string Obj { get; }
        public string Act { get; }
        public string Field { get; set; }

        public CasbinRequirement(string policyName)
        {
            var split = policyName.Split('_');
            this.Mod = split[0];
            this.Act = split[1];
            this.Obj = split[2];
            this.Field = split.ElementAtOrDefault(3) ?? "";
        }

        public string Mod { get; set; }
    }
}

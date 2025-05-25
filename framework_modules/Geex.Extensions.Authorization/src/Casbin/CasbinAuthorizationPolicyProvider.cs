using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Geex.Extensions.Authorization.Casbin
{
    public class CasbinAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public CasbinAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {

        }


        public override async Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            if (policyName.IsNullOrEmpty())
            {
                return await GetDefaultPolicyAsync() ?? await GetFallbackPolicyAsync();
            }
            return new AuthorizationPolicy(new[]
            {
                new CasbinRequirement(policyName)
            }, new[] { JwtBearerDefaults.AuthenticationScheme, "SuperAdmin", "AuthenticationTypes.Federation" });
        }
    }
}

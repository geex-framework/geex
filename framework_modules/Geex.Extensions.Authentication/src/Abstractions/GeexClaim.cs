using System.Security.Claims;

namespace Geex.Extensions.Authentication
{
    public class GeexClaim : Claim
    {
        protected GeexClaim(Claim other) : base(other)
        {
        }

        public GeexClaim(GeexClaimType type, string value) : base(type, value)
        {
        }
    }
}

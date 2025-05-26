using System.Security.Claims;

namespace Geex
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

using System.Security.Claims;

namespace Geex
{
    public class GeexClaim : Claim
    {
        public static GeexClaim AdminClaim = new GeexClaim(GeexClaimType.Sub, "000000000000000000000001");
        protected GeexClaim(Claim other) : base(other)
        {
        }

        public GeexClaim(GeexClaimType type, string value) : base(type, value)
        {
        }
    }
}

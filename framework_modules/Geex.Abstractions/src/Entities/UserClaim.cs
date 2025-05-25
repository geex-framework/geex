namespace Geex.Abstractions.Entities
{
    public class UserClaim
    {
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }

        public UserClaim(string claimType, string claimValue)
        {
            this.ClaimType = claimType;
            ClaimValue = claimValue;
        }

    }
}

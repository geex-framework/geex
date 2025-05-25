namespace Geex.Abstractions.Enumerations
{
    public class GeexClaimType : Enumeration<GeexClaimType>
    {
        public const string _Sub = "sub";
        public static GeexClaimType Sub { get; } = new GeexClaimType(nameof(Sub), _Sub);

        public const string _ClientId = "client_id";
        public static GeexClaimType ClientId { get; } = new GeexClaimType(nameof(ClientId), _ClientId);
        public const string _Tenant = "__tenant";
        public static GeexClaimType Tenant { get; } = new GeexClaimType(nameof(Tenant), _Tenant);
        public const string _Nickname = "nick_name";
        public static GeexClaimType Nickname { get; } = new GeexClaimType(nameof(Nickname), _Nickname);
        public const string _Provider = "login_provider";
        public static GeexClaimType Provider { get; } = new GeexClaimType(nameof(Provider), _Provider);
        public const string _Expires = "expires";
        public static GeexClaimType Expires { get; } = new GeexClaimType(nameof(Expires), _Expires);
        public const string _FullName = "fullname";
        public static GeexClaimType FullName { get; } = new GeexClaimType(nameof(FullName), _FullName);
        public const string _Org = "org";
        public static GeexClaimType Org { get; } = new GeexClaimType(nameof(Org), _Org);
        public const string _Role = "role";
        public static GeexClaimType Role { get; } = new GeexClaimType(nameof(Role), _Role);

        public GeexClaimType(string name, string value) : base(name, value)
        {
        }

    }
}

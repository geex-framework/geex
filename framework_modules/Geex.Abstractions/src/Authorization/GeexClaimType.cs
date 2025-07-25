namespace Geex
{
    public class GeexClaimType : Enumeration<GeexClaimType>
    {
        public const string _Sub = "sub";
        public static GeexClaimType Sub { get; } = FromNameAndValue(nameof(Sub), _Sub);

        public const string _ClientId = "client_id";
        public static GeexClaimType ClientId { get; } = FromNameAndValue(nameof(ClientId), _ClientId);
        public const string _Tenant = "__tenant";
        public static GeexClaimType Tenant { get; } = FromNameAndValue(nameof(Tenant), _Tenant);
        public const string _Nickname = "nick_name";
        public static GeexClaimType Nickname { get; } = FromNameAndValue(nameof(Nickname), _Nickname);
        public const string _Provider = "login_provider";
        public static GeexClaimType Provider { get; } = FromNameAndValue(nameof(Provider), _Provider);
        public const string _Expires = "expires";
        public static GeexClaimType Expires { get; } = FromNameAndValue(nameof(Expires), _Expires);
        public const string _FullName = "fullname";
        public static GeexClaimType FullName { get; } = FromNameAndValue(nameof(FullName), _FullName);
        public const string _Org = "org";
        public static GeexClaimType Org { get; } = FromNameAndValue(nameof(Org), _Org);
        public const string _Role = "role";
        public static GeexClaimType Role { get; } = FromNameAndValue(nameof(Role), _Role);
    }
}

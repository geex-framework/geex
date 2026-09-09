namespace Geex.Extensions.Identity
{
    public class OrgTypeEnum : Enumeration<OrgTypeEnum>
    {
        public static OrgTypeEnum Default { get; } = FromValue(nameof(Default));
        public static OrgTypeEnum Company { get; } = FromValue(nameof(Company));
        public static OrgTypeEnum Partner { get; } = FromValue(nameof(Partner));
    }
}

using Geex.Abstractions;

namespace Geex.Abstractions.Entities
{
    public class OrgTypeEnum : Enumeration<OrgTypeEnum>
    {
        public OrgTypeEnum(string value) : base(value)
        {

        }
        public static OrgTypeEnum Default { get; } = new OrgTypeEnum(nameof(Default));

    }
}

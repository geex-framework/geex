using Geex.Common.Abstractions;

namespace Geex.Common.Abstraction.Enumerations;

public class ChangeDetectDataType : Enumeration<ChangeDetectDataType>
{
    public ChangeDetectDataType(string name, string value) : base(name, value)
    {

    }
    public static ChangeDetectDataType Org { get; } = new(nameof(Org), nameof(Org));
    public static ChangeDetectDataType Tenant { get; } = new(nameof(Tenant), nameof(Tenant));
    public static ChangeDetectDataType Role { get; } = new(nameof(Role), nameof(Role));
    public static ChangeDetectDataType User { get; } = new(nameof(User), nameof(User));

}

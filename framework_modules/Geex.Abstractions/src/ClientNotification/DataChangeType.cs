namespace Geex.Abstractions.ClientNotification;

public class DataChangeType : Enumeration<DataChangeType>
{
    public DataChangeType(string name, string value) : base(name, value)
    {

    }
    public static DataChangeType Org { get; } = new(nameof(Org), nameof(Org));
    public static DataChangeType Tenant { get; } = new(nameof(Tenant), nameof(Tenant));
    public static DataChangeType Role { get; } = new(nameof(Role), nameof(Role));
    public static DataChangeType User { get; } = new(nameof(User), nameof(User));

}

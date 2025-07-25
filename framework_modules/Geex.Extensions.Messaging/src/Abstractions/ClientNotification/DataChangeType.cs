namespace Geex.Extensions.Messaging.ClientNotification;

public class DataChangeType : Enumeration<DataChangeType>
{
    public static DataChangeType Org { get; } = FromValue(nameof(Org));
    public static DataChangeType Tenant { get; } = FromValue(nameof(Tenant));
    public static DataChangeType Role { get; } = FromValue(nameof(Role));
    public static DataChangeType User { get; } = FromValue(nameof(User));

}

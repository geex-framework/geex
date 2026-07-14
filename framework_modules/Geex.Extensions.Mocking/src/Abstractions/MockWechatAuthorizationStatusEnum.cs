namespace Geex.Extensions.Mocking;

public class MockWechatAuthorizationStatusEnum : Enumeration<MockWechatAuthorizationStatusEnum>
{
    public static MockWechatAuthorizationStatusEnum Pending { get; } = FromValue(nameof(Pending));
    public static MockWechatAuthorizationStatusEnum Confirmed { get; } = FromValue(nameof(Confirmed));
    public static MockWechatAuthorizationStatusEnum Consumed { get; } = FromValue(nameof(Consumed));
    public static MockWechatAuthorizationStatusEnum Expired { get; } = FromValue(nameof(Expired));
}

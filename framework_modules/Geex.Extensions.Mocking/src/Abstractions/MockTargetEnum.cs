namespace Geex.Extensions.Mocking;

public class MockTargetEnum : Enumeration<MockTargetEnum>
{
    public static MockTargetEnum WechatApiClient { get; } = FromValue(nameof(WechatApiClient));
    public static MockTargetEnum SmsSender { get; } = FromValue(nameof(SmsSender));
    public static MockTargetEnum PaymentProvider { get; } = FromValue(nameof(PaymentProvider));
    public static MockTargetEnum ExternalTenantSyncProvider { get; } = FromValue(nameof(ExternalTenantSyncProvider));
}

namespace Geex.Extensions.Mocking;

public class MockInteractionPurposeEnum : Enumeration<MockInteractionPurposeEnum>
{
    public static MockInteractionPurposeEnum WechatAuthorize { get; } = FromValue(nameof(WechatAuthorize));
    public static MockInteractionPurposeEnum PaymentCheckout { get; } = FromValue(nameof(PaymentCheckout));
}

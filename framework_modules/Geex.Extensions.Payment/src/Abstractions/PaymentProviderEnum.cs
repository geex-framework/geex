namespace Geex.Extensions.Payment;

public class PaymentProviderEnum : Enumeration<PaymentProviderEnum>
{
    public static PaymentProviderEnum WeChatPay { get; } = FromValue(nameof(WeChatPay));
    public static PaymentProviderEnum Alipay { get; } = FromValue(nameof(Alipay));
    public static PaymentProviderEnum Mock { get; } = FromValue(nameof(Mock));
}

namespace Geex.Extensions.Payment;

public class PaymentChannelEnum : Enumeration<PaymentChannelEnum>
{
    public static PaymentChannelEnum Native { get; } = FromValue(nameof(Native));
    public static PaymentChannelEnum JsApi { get; } = FromValue(nameof(JsApi));
    public static PaymentChannelEnum PcWeb { get; } = FromValue(nameof(PcWeb));
}

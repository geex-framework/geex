namespace Geex.Extensions.Payments;

public class PaymentChannelEnum : Enumeration<PaymentChannelEnum>
{
    public static PaymentChannelEnum Precreate { get; } = FromValue(nameof(Precreate));
    public static PaymentChannelEnum Pay { get; } = FromValue(nameof(Pay));
}

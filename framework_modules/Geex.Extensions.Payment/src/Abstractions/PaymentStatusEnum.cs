namespace Geex.Extensions.Payment;

public class PaymentStatusEnum : Enumeration<PaymentStatusEnum>
{
    public static PaymentStatusEnum Pending { get; } = FromValue(nameof(Pending));
    public static PaymentStatusEnum Paying { get; } = FromValue(nameof(Paying));
    public static PaymentStatusEnum Succeeded { get; } = FromValue(nameof(Succeeded));
    public static PaymentStatusEnum Failed { get; } = FromValue(nameof(Failed));
    public static PaymentStatusEnum Closed { get; } = FromValue(nameof(Closed));
}

namespace Geex.Extensions.Payments;

public class PaymentRefundStatusEnum : Enumeration<PaymentRefundStatusEnum>
{
    public static PaymentRefundStatusEnum Pending { get; } = FromValue(nameof(Pending));
    public static PaymentRefundStatusEnum Processing { get; } = FromValue(nameof(Processing));
    public static PaymentRefundStatusEnum Succeeded { get; } = FromValue(nameof(Succeeded));
    public static PaymentRefundStatusEnum Failed { get; } = FromValue(nameof(Failed));
}

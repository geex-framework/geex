namespace Geex.Extensions.Mocking;

public class MockPaymentTransactionStatusEnum : Enumeration<MockPaymentTransactionStatusEnum>
{
    public static MockPaymentTransactionStatusEnum Paying { get; } = FromValue(nameof(Paying));
    public static MockPaymentTransactionStatusEnum Succeeded { get; } = FromValue(nameof(Succeeded));
    public static MockPaymentTransactionStatusEnum Failed { get; } = FromValue(nameof(Failed));
    public static MockPaymentTransactionStatusEnum Closed { get; } = FromValue(nameof(Closed));
    public static MockPaymentTransactionStatusEnum Revoked { get; } = FromValue(nameof(Revoked));
}

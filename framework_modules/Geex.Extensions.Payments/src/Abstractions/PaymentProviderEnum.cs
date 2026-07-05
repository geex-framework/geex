namespace Geex.Extensions.Payments;

public class PaymentProviderEnum : Enumeration<PaymentProviderEnum>
{
    public static PaymentProviderEnum Virtual { get; } = FromValue(nameof(Virtual));
    public static PaymentProviderEnum Shouqianba { get; } = FromValue(nameof(Shouqianba));
}

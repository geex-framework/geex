namespace Geex.Extensions.Payment.Requests;

public class CreatePaymentOrderResult
{
    public IPaymentOrder Order { get; set; } = default!;
    public PaymentPrepayResult Prepay { get; set; } = new();
}

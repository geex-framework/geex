namespace Geex.Extensions.Payment;

public class PaymentQueryResult
{
    public PaymentStatusEnum Status { get; set; } = PaymentStatusEnum.Pending;
    public string? TransactionId { get; set; }
}

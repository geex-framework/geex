namespace Geex.Extensions.Payment;

public class PaymentCallbackResult
{
    public bool Success { get; set; }
    public string? OutTradeNo { get; set; }
    public string? TransactionId { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/json";
}

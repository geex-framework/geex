namespace Geex.Extensions.Payments;

public class PaymentPrepayResult
{
    public string OutTradeNo { get; set; } = string.Empty;
    public string? PrepayId { get; set; }
    public string? CodeUrl { get; set; }
    public string? TradeNo { get; set; }
}

public class PaymentQueryResult
{
    public PaymentStatusEnum Status { get; set; } = PaymentStatusEnum.Pending;
    public string? TransactionId { get; set; }
    public string? TradeNo { get; set; }
}

public class PaymentProviderResult
{
    public bool Success { get; set; }
    public string? TradeNo { get; set; }
    public string? Message { get; set; }
}

public class PaymentRefundResult
{
    public bool Success { get; set; }
    public string? RefundTradeNo { get; set; }
    public string? TradeNo { get; set; }
    public string? Message { get; set; }
}

public class PaymentRefundQueryResult
{
    public PaymentRefundStatusEnum Status { get; set; } = PaymentRefundStatusEnum.Pending;
    public string? RefundTradeNo { get; set; }
    public string? TradeNo { get; set; }
}

public class PaymentCallbackResult
{
    public bool Success { get; set; }
    public string? ClientSn { get; set; }
    public string? TransactionId { get; set; }
    public string ResponseBody { get; set; } = "success";
    public string ContentType { get; set; } = "text/plain";
}

public class PaymentCreateContext
{
    public string? AuthCode { get; set; }
    public string? NotifyUrl { get; set; }
    public string? RefundNotifyUrl { get; set; }
}

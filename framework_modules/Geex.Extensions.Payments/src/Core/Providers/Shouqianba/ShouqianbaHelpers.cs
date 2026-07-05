namespace Geex.Extensions.Payments.Core.Providers.Shouqianba;

public class ShouqianbaCredentials
{
    public string TerminalSn { get; init; } = string.Empty;
    public string TerminalKey { get; init; } = string.Empty;
}

internal static class ShouqianbaAmount
{
    public static string ToFen(decimal amount) => ((long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero)).ToString();

    public static decimal FromFen(string? fen)
        => decimal.TryParse(fen, out var value) ? value / 100m : 0m;
}

internal static class ShouqianbaOrderStatusMapper
{
    public static PaymentStatusEnum MapPaymentStatus(string? orderStatus)
        => orderStatus switch
        {
            "PAID" => PaymentStatusEnum.Succeeded,
            "PAY_CANCELED" => PaymentStatusEnum.Closed,
            "REFUNDED" => PaymentStatusEnum.Succeeded,
            "PARTIAL_REFUNDED" => PaymentStatusEnum.Succeeded,
            "CANCELED" => PaymentStatusEnum.Revoked,
            "PAY_ERROR" => PaymentStatusEnum.Failed,
            "CREATED" => PaymentStatusEnum.Paying,
            _ => PaymentStatusEnum.Paying,
        };

    public static PaymentRefundStatusEnum MapRefundStatus(string? resultCode)
        => resultCode switch
        {
            "REFUND_SUCCESS" => PaymentRefundStatusEnum.Succeeded,
            "REFUND_ERROR" => PaymentRefundStatusEnum.Failed,
            "REFUND_INPROGRESS" => PaymentRefundStatusEnum.Processing,
            _ => PaymentRefundStatusEnum.Processing,
        };
}

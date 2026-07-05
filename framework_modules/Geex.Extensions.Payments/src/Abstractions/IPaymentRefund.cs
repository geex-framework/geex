using MongoDB.Entities;

namespace Geex.Extensions.Payments;

public interface IPaymentRefund : IEntityBase
{
    string PaymentId { get; }
    string ClientSn { get; }
    string RefundRequestNo { get; }
    decimal Amount { get; }
    PaymentRefundStatusEnum Status { get; }
    string? TradeNo { get; }
    string? RefundTradeNo { get; }
    DateTimeOffset? FinishedAt { get; }
    string TenantCode { get; }
}

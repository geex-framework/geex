using MongoDB.Entities;

namespace Geex.Extensions.Payments;

public interface IPayment : IEntityBase
{
    string ClientSn { get; }
    string? BusinessOrderId { get; }
    PaymentProviderEnum Provider { get; }
    PaymentChannelEnum Channel { get; }
    PaymentStatusEnum Status { get; }
    decimal Amount { get; }
    decimal RefundedAmount { get; }
    string Currency { get; }
    string Subject { get; }
    string? PrepayId { get; }
    string? TradeNo { get; }
    string? TransactionId { get; }
    DateTimeOffset? PaidAt { get; }
    DateTimeOffset? ExpireAt { get; }
    string TenantCode { get; }
}

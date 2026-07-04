using System.Text.Json.Nodes;
using MongoDB.Entities;

namespace Geex.Extensions.Payment;

public interface IPaymentOrder : IEntityBase
{
    string OutTradeNo { get; }
    string? BusinessOrderId { get; }
    PaymentProviderEnum Provider { get; }
    PaymentChannelEnum Channel { get; }
    PaymentStatusEnum Status { get; }
    decimal Amount { get; }
    string Currency { get; }
    string Subject { get; }
    string? PrepayId { get; }
    string? TransactionId { get; }
    DateTimeOffset? PaidAt { get; }
    JsonNode? ExtraData { get; }
}

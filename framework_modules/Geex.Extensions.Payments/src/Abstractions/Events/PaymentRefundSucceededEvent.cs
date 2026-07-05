using MediatX;

namespace Geex.Extensions.Payments.Events;

public record PaymentRefundSucceededEvent(string ClientSn, string RefundRequestNo, decimal Amount) : IEvent;

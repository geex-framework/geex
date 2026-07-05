using MediatX;

namespace Geex.Extensions.Payments.Events;

public record PaymentSucceededEvent(string ClientSn, string? BusinessOrderId, decimal Amount, PaymentProviderEnum Provider) : IEvent;

using MediatX;

namespace Geex.Extensions.Payment.Events;

public class PaymentSucceededEvent : IEvent
{
    public string OutTradeNo { get; }
    public string? BusinessOrderId { get; }
    public decimal Amount { get; }
    public PaymentProviderEnum Provider { get; }

    public PaymentSucceededEvent(string outTradeNo, string? businessOrderId, decimal amount, PaymentProviderEnum provider)
    {
        OutTradeNo = outTradeNo;
        BusinessOrderId = businessOrderId;
        Amount = amount;
        Provider = provider;
    }
}

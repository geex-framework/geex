using MediatX;

namespace Geex.Extensions.Payments.Requests;

public record CreatePaymentRequest : IRequest<CreatePaymentResult>
{
    public decimal Amount { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string? BusinessOrderId { get; init; }
    public PaymentProviderEnum? Provider { get; init; }
    public PaymentChannelEnum Channel { get; init; } = PaymentChannelEnum.Precreate;
    public string? AuthCode { get; init; }
}

public record CreatePaymentResult(IPayment Payment, PaymentPrepayResult Prepay);

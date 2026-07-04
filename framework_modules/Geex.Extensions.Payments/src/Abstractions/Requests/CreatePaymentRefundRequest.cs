using MediatX;

namespace Geex.Extensions.Payments.Requests;

public record CreatePaymentRefundRequest : IRequest<IPaymentRefund>
{
    public string ClientSn { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? RefundRequestNo { get; init; }
}

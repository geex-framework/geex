using MediatX;

namespace Geex.Extensions.Payments.Requests;

public record CompletePaymentRefundRequest(string RefundRequestNo, string? RefundTradeNo) : IRequest<IPaymentRefund>;

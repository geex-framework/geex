using MediatX;

namespace Geex.Extensions.Payments.Requests;

public record GetPaymentRefundRequest(string RefundRequestNo) : IRequest<IPaymentRefund?>;

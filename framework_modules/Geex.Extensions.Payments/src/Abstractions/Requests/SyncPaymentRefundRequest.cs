using MediatX;

namespace Geex.Extensions.Payments.Requests;

public record SyncPaymentRefundRequest(string RefundRequestNo) : IRequest<IPaymentRefund>;

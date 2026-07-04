using MediatX;

namespace Geex.Extensions.Payments.Requests;

public record ClosePaymentRequest(string ClientSn) : IRequest<IPayment>;

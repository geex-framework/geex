using MediatX;

namespace Geex.Extensions.Payments.Requests;

public record RevokePaymentRequest(string ClientSn) : IRequest<IPayment>;

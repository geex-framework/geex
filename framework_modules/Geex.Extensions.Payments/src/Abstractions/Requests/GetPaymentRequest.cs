using MediatX;

namespace Geex.Extensions.Payments.Requests;

public record GetPaymentRequest(string ClientSn) : IRequest<IPayment?>;

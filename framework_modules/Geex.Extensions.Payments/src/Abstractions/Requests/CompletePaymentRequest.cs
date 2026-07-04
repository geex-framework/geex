using MediatX;

namespace Geex.Extensions.Payments.Requests;

public record CompletePaymentRequest(string ClientSn, string? TransactionId) : IRequest<IPayment>;
